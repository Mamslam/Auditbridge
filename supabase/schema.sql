-- =============================================
-- AuditBridge — Universal Audit Platform Schema
-- Moteur d'audit agnostique au référentiel
-- =============================================

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE OR REPLACE FUNCTION current_org_id() RETURNS UUID AS $$
  SELECT NULLIF(current_setting('app.current_org_id', true), '')::UUID;
$$ LANGUAGE sql STABLE;

-- =============================================
-- CORE : Organisations & Utilisateurs
-- =============================================

CREATE TABLE organizations (
  id                     UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name                   VARCHAR(255) NOT NULL,
  type                   TEXT NOT NULL CHECK (type IN ('auditor', 'client')),
  plan                   VARCHAR(20) DEFAULT 'starter',
  stripe_customer_id     VARCHAR(255),
  stripe_subscription_id VARCHAR(255),
  country_code           CHAR(2) NOT NULL,
  language               VARCHAR(10) DEFAULT 'fr',
  logo_url               TEXT,
  is_active              BOOLEAN DEFAULT TRUE,
  created_at             TIMESTAMPTZ DEFAULT now(),
  updated_at             TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE users (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  clerk_id        TEXT NOT NULL UNIQUE,
  organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
  email           TEXT NOT NULL,
  full_name       TEXT,
  role            TEXT NOT NULL DEFAULT 'auditor_lead',
  is_active       BOOLEAN DEFAULT TRUE,
  last_login_at   TIMESTAMPTZ,
  created_at      TIMESTAMPTZ DEFAULT now(),
  updated_at      TIMESTAMPTZ DEFAULT now()
);

-- =============================================
-- REFERENTIELS (moteur universel)
-- =============================================

CREATE TABLE referential_categories (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  slug        TEXT NOT NULL UNIQUE,
  label       TEXT NOT NULL,
  color_hex   TEXT NOT NULL DEFAULT '#6B7280',
  icon        TEXT,
  created_at  TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE referentials (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  org_id      UUID REFERENCES organizations(id) ON DELETE CASCADE,
  category_id UUID REFERENCES referential_categories(id),
  code        TEXT NOT NULL,
  name        TEXT NOT NULL,
  version     TEXT,
  description TEXT,
  is_system   BOOLEAN DEFAULT FALSE,
  is_public   BOOLEAN DEFAULT FALSE,
  metadata    JSONB DEFAULT '{}',
  created_at  TIMESTAMPTZ DEFAULT now(),
  updated_at  TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE template_sections (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  referential_id UUID NOT NULL REFERENCES referentials(id) ON DELETE CASCADE,
  parent_id      UUID REFERENCES template_sections(id),
  order_index    INTEGER NOT NULL DEFAULT 0,
  code           TEXT,
  title          TEXT NOT NULL,
  description    TEXT,
  created_at     TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE template_questions (
  id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  referential_id    UUID NOT NULL REFERENCES referentials(id) ON DELETE CASCADE,
  section_id        UUID REFERENCES template_sections(id),
  order_index       INTEGER NOT NULL DEFAULT 0,
  code              TEXT,
  question          TEXT NOT NULL,
  guidance          TEXT,
  answer_type       TEXT NOT NULL DEFAULT 'text',
  answer_options    JSONB,
  is_mandatory      BOOLEAN DEFAULT TRUE,
  criticality       TEXT DEFAULT 'major',
  expected_evidence TEXT[],
  tags              TEXT[],
  metadata          JSONB DEFAULT '{}',
  created_at        TIMESTAMPTZ DEFAULT now()
);

-- =============================================
-- AUDITS
-- =============================================

CREATE TABLE audits (
  id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  org_id                  UUID NOT NULL REFERENCES organizations(id),
  referential_id          UUID NOT NULL REFERENCES referentials(id),
  template_snapshot       JSONB NOT NULL DEFAULT '{}',
  title                   TEXT NOT NULL,
  description             TEXT,
  status                  TEXT NOT NULL DEFAULT 'draft',
  auditor_id              UUID NOT NULL REFERENCES users(id),
  client_org_name         TEXT NOT NULL,
  client_email            TEXT NOT NULL,
  client_token            TEXT UNIQUE,
  client_token_expires_at TIMESTAMPTZ,
  due_date                DATE,
  scope                   TEXT,
  metadata                JSONB DEFAULT '{}',
  created_at              TIMESTAMPTZ DEFAULT now(),
  updated_at              TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE audit_responses (
  id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  audit_id           UUID NOT NULL REFERENCES audits(id) ON DELETE CASCADE,
  question_id        UUID NOT NULL REFERENCES template_questions(id),
  answered_by        UUID REFERENCES users(id),
  answered_by_client BOOLEAN DEFAULT FALSE,
  answer_value       TEXT,
  answer_notes       TEXT,
  conformity         TEXT,
  auditor_comment    TEXT,
  is_flagged         BOOLEAN DEFAULT FALSE,
  ai_analysis        JSONB,
  created_at         TIMESTAMPTZ DEFAULT now(),
  updated_at         TIMESTAMPTZ DEFAULT now(),
  UNIQUE(audit_id, question_id)
);

CREATE TABLE audit_evidence (
  id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  audit_id           UUID NOT NULL REFERENCES audits(id) ON DELETE CASCADE,
  response_id        UUID REFERENCES audit_responses(id),
  question_id        UUID REFERENCES template_questions(id),
  uploaded_by        UUID REFERENCES users(id),
  uploaded_by_client BOOLEAN DEFAULT FALSE,
  file_name          TEXT NOT NULL,
  file_size          INTEGER,
  mime_type          TEXT,
  storage_path       TEXT NOT NULL,
  sha256_hash        TEXT NOT NULL,
  is_watermarked     BOOLEAN DEFAULT FALSE,
  metadata           JSONB DEFAULT '{}',
  created_at         TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE audit_capas (
  id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  audit_id          UUID NOT NULL REFERENCES audits(id) ON DELETE CASCADE,
  response_id       UUID REFERENCES audit_responses(id),
  question_id       UUID REFERENCES template_questions(id),
  title             TEXT NOT NULL,
  description       TEXT,
  root_cause        TEXT,
  action_type       TEXT DEFAULT 'corrective',
  priority          TEXT DEFAULT 'high',
  status            TEXT DEFAULT 'open',
  assigned_to_email TEXT,
  due_date          DATE,
  completed_at      TIMESTAMPTZ,
  verified_by       UUID REFERENCES users(id),
  evidence_path     TEXT,
  ai_generated      BOOLEAN DEFAULT FALSE,
  metadata          JSONB DEFAULT '{}',
  created_at        TIMESTAMPTZ DEFAULT now(),
  updated_at        TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE audit_reports (
  id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  audit_id          UUID UNIQUE NOT NULL REFERENCES audits(id),
  generated_by      UUID REFERENCES users(id),
  generated_at      TIMESTAMPTZ DEFAULT now(),
  conformity_score  DECIMAL(5,2),
  total_questions   INTEGER,
  conform_count     INTEGER,
  non_conform_count INTEGER,
  partial_count     INTEGER,
  na_count          INTEGER,
  critical_nc       INTEGER DEFAULT 0,
  major_nc          INTEGER DEFAULT 0,
  minor_nc          INTEGER DEFAULT 0,
  executive_summary TEXT,
  ai_narrative      TEXT,
  report_data       JSONB NOT NULL DEFAULT '{}',
  pdf_storage_path  TEXT,
  pdf_sha256        TEXT,
  version           INTEGER DEFAULT 1,
  metadata          JSONB DEFAULT '{}'
);

CREATE TABLE audit_trail (
  id          BIGSERIAL PRIMARY KEY,
  tenant_id   UUID NOT NULL,
  actor_id    UUID,
  actor_type  TEXT,
  action      TEXT NOT NULL,
  entity_type TEXT NOT NULL,
  entity_id   UUID NOT NULL,
  old_values  JSONB,
  new_values  JSONB,
  ip_address  TEXT,
  user_agent  TEXT,
  created_at  TIMESTAMPTZ DEFAULT now()
);

-- =============================================
-- INDEXES
-- =============================================

CREATE INDEX idx_referentials_org ON referentials(org_id) WHERE org_id IS NOT NULL;
CREATE INDEX idx_referentials_system ON referentials(is_system) WHERE is_system = TRUE;
CREATE INDEX idx_template_sections_ref ON template_sections(referential_id);
CREATE INDEX idx_template_questions_ref ON template_questions(referential_id);
CREATE INDEX idx_template_questions_section ON template_questions(section_id);
CREATE INDEX idx_audits_org ON audits(org_id);
CREATE INDEX idx_audits_referential ON audits(referential_id);
CREATE INDEX idx_audits_status ON audits(status);
CREATE INDEX idx_audit_responses_audit ON audit_responses(audit_id);
CREATE INDEX idx_audit_evidence_audit ON audit_evidence(audit_id);
CREATE INDEX idx_audit_capas_audit ON audit_capas(audit_id);
CREATE INDEX idx_audit_trail_tenant ON audit_trail(tenant_id);

-- =============================================
-- TRIGGERS : updated_at
-- =============================================

CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN NEW.updated_at = now(); RETURN NEW; END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_organizations_updated_at BEFORE UPDATE ON organizations FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_users_updated_at BEFORE UPDATE ON users FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_referentials_updated_at BEFORE UPDATE ON referentials FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_audits_updated_at BEFORE UPDATE ON audits FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_audit_responses_updated_at BEFORE UPDATE ON audit_responses FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_audit_capas_updated_at BEFORE UPDATE ON audit_capas FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- Audit trail immutable
CREATE OR REPLACE FUNCTION prevent_audit_trail_modification()
RETURNS TRIGGER AS $$
BEGIN RAISE EXCEPTION 'audit_trail is immutable'; END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_audit_trail_immutable
  BEFORE UPDATE OR DELETE ON audit_trail
  FOR EACH ROW EXECUTE FUNCTION prevent_audit_trail_modification();

-- =============================================
-- RLS
-- =============================================

ALTER TABLE organizations ENABLE ROW LEVEL SECURITY;
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE referentials ENABLE ROW LEVEL SECURITY;
ALTER TABLE template_sections ENABLE ROW LEVEL SECURITY;
ALTER TABLE template_questions ENABLE ROW LEVEL SECURITY;
ALTER TABLE audits ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit_responses ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit_evidence ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit_capas ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit_reports ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit_trail ENABLE ROW LEVEL SECURITY;

-- Organizations
CREATE POLICY "org_read"   ON organizations FOR SELECT USING (id = current_org_id());
CREATE POLICY "org_insert" ON organizations FOR INSERT WITH CHECK (true);
CREATE POLICY "org_update" ON organizations FOR UPDATE USING (id = current_org_id());

-- Users
CREATE POLICY "users_read"   ON users FOR SELECT USING (organization_id = current_org_id());
CREATE POLICY "users_insert" ON users FOR INSERT WITH CHECK (true);
CREATE POLICY "users_update" ON users FOR UPDATE USING (organization_id = current_org_id());

-- Referentials: système lisibles par tous, custom par leur org
CREATE POLICY "referentials_read"   ON referentials FOR SELECT USING (org_id IS NULL OR org_id = current_org_id());
CREATE POLICY "referentials_insert" ON referentials FOR INSERT WITH CHECK (org_id = current_org_id() OR org_id IS NULL);
CREATE POLICY "referentials_update" ON referentials FOR UPDATE USING (org_id = current_org_id() AND is_system = FALSE);
CREATE POLICY "referentials_delete" ON referentials FOR DELETE USING (org_id = current_org_id() AND is_system = FALSE);

-- Template sections & questions
CREATE POLICY "sections_read"   ON template_sections FOR SELECT USING (referential_id IN (SELECT id FROM referentials WHERE org_id IS NULL OR org_id = current_org_id()));
CREATE POLICY "sections_insert" ON template_sections FOR INSERT WITH CHECK (true);
CREATE POLICY "sections_write"  ON template_sections FOR ALL USING (referential_id IN (SELECT id FROM referentials WHERE org_id = current_org_id() AND is_system = FALSE));

CREATE POLICY "questions_read"   ON template_questions FOR SELECT USING (referential_id IN (SELECT id FROM referentials WHERE org_id IS NULL OR org_id = current_org_id()));
CREATE POLICY "questions_insert" ON template_questions FOR INSERT WITH CHECK (true);
CREATE POLICY "questions_write"  ON template_questions FOR ALL USING (referential_id IN (SELECT id FROM referentials WHERE org_id = current_org_id() AND is_system = FALSE));

-- Audits
CREATE POLICY "audits_read"   ON audits FOR SELECT USING (org_id = current_org_id());
CREATE POLICY "audits_insert" ON audits FOR INSERT WITH CHECK (org_id = current_org_id());
CREATE POLICY "audits_update" ON audits FOR UPDATE USING (org_id = current_org_id());

-- Audit sub-tables
CREATE POLICY "responses_read"   ON audit_responses FOR SELECT USING (audit_id IN (SELECT id FROM audits WHERE org_id = current_org_id()));
CREATE POLICY "responses_insert" ON audit_responses FOR INSERT WITH CHECK (true);
CREATE POLICY "responses_write"  ON audit_responses FOR ALL USING (audit_id IN (SELECT id FROM audits WHERE org_id = current_org_id()));

CREATE POLICY "evidence_read"   ON audit_evidence FOR SELECT USING (audit_id IN (SELECT id FROM audits WHERE org_id = current_org_id()));
CREATE POLICY "evidence_insert" ON audit_evidence FOR INSERT WITH CHECK (true);

CREATE POLICY "capas_read"   ON audit_capas FOR SELECT USING (audit_id IN (SELECT id FROM audits WHERE org_id = current_org_id()));
CREATE POLICY "capas_insert" ON audit_capas FOR INSERT WITH CHECK (true);
CREATE POLICY "capas_write"  ON audit_capas FOR ALL USING (audit_id IN (SELECT id FROM audits WHERE org_id = current_org_id()));

CREATE POLICY "reports_read"   ON audit_reports FOR SELECT USING (audit_id IN (SELECT id FROM audits WHERE org_id = current_org_id()));
CREATE POLICY "reports_insert" ON audit_reports FOR INSERT WITH CHECK (true);

-- Audit trail
CREATE POLICY "trail_read"   ON audit_trail FOR SELECT USING (tenant_id = current_org_id());
CREATE POLICY "trail_insert" ON audit_trail FOR INSERT WITH CHECK (true);

-- ============================================================
-- AUDITBRIDGE — Schéma PostgreSQL complet avec RLS
-- À exécuter dans Supabase SQL Editor
-- ============================================================

-- Extensions
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "pg_trgm"; -- Pour la recherche full-text

-- ============================================================
-- TABLES
-- ============================================================

-- Multi-tenancy : chaque organisation est isolée
CREATE TABLE organizations (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name VARCHAR(255) NOT NULL,
  type VARCHAR(20) NOT NULL CHECK (type IN ('auditor', 'client')),
  plan VARCHAR(20) NOT NULL DEFAULT 'starter',
  stripe_customer_id VARCHAR(255),
  stripe_subscription_id VARCHAR(255),
  country_code CHAR(2) NOT NULL,
  language VARCHAR(10) NOT NULL DEFAULT 'fr',
  logo_url TEXT,
  is_active BOOLEAN DEFAULT true,
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Utilisateurs liés à une organisation
CREATE TABLE users (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  clerk_id VARCHAR(255) UNIQUE NOT NULL,
  organization_id UUID REFERENCES organizations(id),
  email VARCHAR(255) NOT NULL,
  full_name VARCHAR(255),
  role VARCHAR(30) NOT NULL CHECK (role IN (
    'auditor_lead', 'auditor_junior', 'auditor_viewer',
    'client_admin', 'client_contributor', 'client_viewer',
    'platform_admin'
  )),
  is_active BOOLEAN DEFAULT true,
  last_login_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_users_clerk_id ON users(clerk_id);
CREATE INDEX idx_users_organization_id ON users(organization_id);

-- Templates d'audit
CREATE TABLE audit_templates (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id UUID REFERENCES organizations(id),
  name VARCHAR(255) NOT NULL,
  framework VARCHAR(50) NOT NULL CHECK (framework IN (
    'GMP', 'EU_GMP', 'ISO_9001', 'ISO_27001', 'ISO_14001',
    'NIS2', 'RGPD', 'HACCP', 'CSRD', 'DORA', 'CUSTOM'
  )),
  version VARCHAR(20) NOT NULL DEFAULT '1.0',
  language VARCHAR(10) NOT NULL DEFAULT 'fr',
  description TEXT,
  is_public BOOLEAN DEFAULT false,
  sections JSONB NOT NULL DEFAULT '[]',
  created_by UUID REFERENCES users(id),
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_audit_templates_org ON audit_templates(organization_id);
CREATE INDEX idx_audit_templates_framework ON audit_templates(framework);

-- Sections d'un template
CREATE TABLE template_sections (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  template_id UUID REFERENCES audit_templates(id) ON DELETE CASCADE,
  title VARCHAR(255) NOT NULL,
  description TEXT,
  order_index INTEGER NOT NULL,
  framework_reference VARCHAR(100),
  is_mandatory BOOLEAN DEFAULT true
);

CREATE INDEX idx_template_sections_template ON template_sections(template_id);

-- Questions d'une section
CREATE TABLE template_questions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  section_id UUID REFERENCES template_sections(id) ON DELETE CASCADE,
  text TEXT NOT NULL,
  type VARCHAR(30) NOT NULL CHECK (type IN (
    'yes_no', 'rating', 'text', 'file_upload', 'multi_select'
  )),
  is_mandatory BOOLEAN DEFAULT true,
  weight INTEGER DEFAULT 1,
  gmp_critical BOOLEAN DEFAULT false,
  regulatory_reference TEXT,
  guidance TEXT,
  order_index INTEGER NOT NULL
);

CREATE INDEX idx_template_questions_section ON template_questions(section_id);

-- Campagnes d'audit
CREATE TABLE audit_campaigns (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  template_id UUID REFERENCES audit_templates(id),
  auditor_org_id UUID REFERENCES organizations(id),
  client_org_id UUID REFERENCES organizations(id),
  lead_auditor_id UUID REFERENCES users(id),
  title VARCHAR(255) NOT NULL,
  status VARCHAR(30) NOT NULL DEFAULT 'draft' CHECK (status IN (
    'draft', 'sent', 'in_progress', 'client_submitted',
    'under_review', 'report_generated', 'closed'
  )),
  scheduled_date DATE,
  deadline DATE,
  scope TEXT,
  client_access_token VARCHAR(255) UNIQUE,
  client_access_expires_at TIMESTAMPTZ,
  compliance_score DECIMAL(5,2),
  ai_analysis JSONB,
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_campaigns_auditor_org ON audit_campaigns(auditor_org_id);
CREATE INDEX idx_campaigns_client_org ON audit_campaigns(client_org_id);
CREATE INDEX idx_campaigns_status ON audit_campaigns(status);
CREATE INDEX idx_campaigns_access_token ON audit_campaigns(client_access_token);

-- Réponses du client
CREATE TABLE audit_responses (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  campaign_id UUID REFERENCES audit_campaigns(id) ON DELETE CASCADE,
  question_id UUID REFERENCES template_questions(id),
  responded_by UUID REFERENCES users(id),
  response_value TEXT,
  response_data JSONB,
  auditor_note TEXT,
  auditor_rating VARCHAR(20) CHECK (auditor_rating IN (
    'compliant', 'minor', 'major', 'critical', 'na'
  )),
  submitted_at TIMESTAMPTZ,
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_responses_campaign ON audit_responses(campaign_id);
CREATE INDEX idx_responses_question ON audit_responses(question_id);

-- Documents / preuves uploadés
CREATE TABLE audit_documents (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  campaign_id UUID REFERENCES audit_campaigns(id) ON DELETE CASCADE,
  question_id UUID REFERENCES template_questions(id),
  uploaded_by UUID REFERENCES users(id),
  file_name VARCHAR(500) NOT NULL,
  file_size BIGINT NOT NULL,
  mime_type VARCHAR(100) NOT NULL,
  storage_path TEXT NOT NULL,
  file_hash VARCHAR(64) NOT NULL,
  is_auditor_only BOOLEAN DEFAULT false,
  watermark_data JSONB,
  uploaded_at TIMESTAMPTZ DEFAULT NOW(),
  is_deleted BOOLEAN DEFAULT false,
  deleted_at TIMESTAMPTZ
);

CREATE INDEX idx_documents_campaign ON audit_documents(campaign_id);
CREATE INDEX idx_documents_question ON audit_documents(question_id);
CREATE INDEX idx_documents_not_deleted ON audit_documents(campaign_id) WHERE NOT is_deleted;

-- Actions correctives (CAPA)
CREATE TABLE capa_actions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  campaign_id UUID REFERENCES audit_campaigns(id) ON DELETE CASCADE,
  response_id UUID REFERENCES audit_responses(id),
  title VARCHAR(500) NOT NULL,
  description TEXT,
  severity VARCHAR(20) NOT NULL CHECK (severity IN ('minor', 'major', 'critical')),
  status VARCHAR(30) NOT NULL DEFAULT 'open' CHECK (status IN (
    'open', 'in_progress', 'pending_verification', 'closed', 'overdue'
  )),
  assigned_to UUID REFERENCES users(id),
  due_date DATE,
  closed_at TIMESTAMPTZ,
  closing_evidence TEXT,
  ai_generated BOOLEAN DEFAULT false,
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_capa_campaign ON capa_actions(campaign_id);
CREATE INDEX idx_capa_status ON capa_actions(status);
CREATE INDEX idx_capa_due_date ON capa_actions(due_date) WHERE status NOT IN ('closed');

-- Rapports d'audit
CREATE TABLE audit_reports (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  campaign_id UUID REFERENCES audit_campaigns(id),
  generated_by UUID REFERENCES users(id),
  version INTEGER NOT NULL DEFAULT 1,
  status VARCHAR(20) NOT NULL DEFAULT 'draft' CHECK (status IN ('draft', 'final', 'signed')),
  storage_path TEXT,
  file_hash VARCHAR(64),
  ai_summary TEXT,
  executive_summary TEXT,
  generated_at TIMESTAMPTZ DEFAULT NOW(),
  signed_at TIMESTAMPTZ,
  signed_by UUID REFERENCES users(id)
);

CREATE INDEX idx_reports_campaign ON audit_reports(campaign_id);

-- Audit trail — IMMUABLE (protégé par trigger)
CREATE TABLE audit_trail (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID REFERENCES users(id),
  organization_id UUID REFERENCES organizations(id),
  action VARCHAR(100) NOT NULL,
  resource_type VARCHAR(50) NOT NULL,
  resource_id UUID,
  campaign_id UUID,
  ip_address TEXT,
  user_agent TEXT,
  metadata JSONB,
  created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_trail_campaign ON audit_trail(campaign_id);
CREATE INDEX idx_trail_user ON audit_trail(user_id);
CREATE INDEX idx_trail_created_at ON audit_trail(created_at DESC);

-- ============================================================
-- TRIGGER : AUDIT TRAIL IMMUABLE
-- ============================================================

CREATE OR REPLACE FUNCTION prevent_audit_trail_modification()
RETURNS TRIGGER AS $$
BEGIN
  RAISE EXCEPTION 'L''audit trail est immuable. Aucune modification autorisée.'
    USING ERRCODE = 'insufficient_privilege';
  RETURN NULL;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

CREATE TRIGGER audit_trail_immutable
  BEFORE UPDATE OR DELETE ON audit_trail
  FOR EACH ROW EXECUTE FUNCTION prevent_audit_trail_modification();

-- ============================================================
-- TRIGGER : updated_at automatique
-- ============================================================

CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
  NEW.updated_at = NOW();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER organizations_updated_at
  BEFORE UPDATE ON organizations
  FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER campaigns_updated_at
  BEFORE UPDATE ON audit_campaigns
  FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER templates_updated_at
  BEFORE UPDATE ON audit_templates
  FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER responses_updated_at
  BEFORE UPDATE ON audit_responses
  FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER capa_updated_at
  BEFORE UPDATE ON capa_actions
  FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ============================================================
-- ROW LEVEL SECURITY (RLS)
-- ============================================================

-- Activer RLS sur toutes les tables sensibles
ALTER TABLE organizations ENABLE ROW LEVEL SECURITY;
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit_templates ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit_campaigns ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit_responses ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit_documents ENABLE ROW LEVEL SECURITY;
ALTER TABLE capa_actions ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit_reports ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit_trail ENABLE ROW LEVEL SECURITY;

-- Helper function pour l'org courante (injectée par le middleware .NET)
CREATE OR REPLACE FUNCTION current_org_id() RETURNS UUID AS $$
BEGIN
  RETURN current_setting('app.current_org_id', true)::UUID;
EXCEPTION
  WHEN OTHERS THEN
    RETURN NULL;
END;
$$ LANGUAGE plpgsql STABLE SECURITY DEFINER;

-- Organizations policies
CREATE POLICY "Org members see their org"
  ON organizations FOR SELECT
  USING (id = current_org_id());

-- Users policies
CREATE POLICY "Users see members of their org"
  ON users FOR SELECT
  USING (organization_id = current_org_id());

CREATE POLICY "Users manage their own record"
  ON users FOR UPDATE
  USING (organization_id = current_org_id());

-- Templates policies
CREATE POLICY "Auditors see their own templates"
  ON audit_templates FOR ALL
  USING (organization_id = current_org_id());

CREATE POLICY "Public templates visible to all"
  ON audit_templates FOR SELECT
  USING (is_public = true);

-- Campaigns policies
CREATE POLICY "Auditors see their campaigns"
  ON audit_campaigns FOR ALL
  USING (auditor_org_id = current_org_id());

CREATE POLICY "Clients see their campaigns (read only)"
  ON audit_campaigns FOR SELECT
  USING (client_org_id = current_org_id());

-- Responses policies
CREATE POLICY "Campaign participants see responses"
  ON audit_responses FOR ALL
  USING (
    campaign_id IN (
      SELECT id FROM audit_campaigns
      WHERE auditor_org_id = current_org_id()
         OR client_org_id = current_org_id()
    )
  );

-- Documents policies
CREATE POLICY "Campaign participants see documents"
  ON audit_documents FOR SELECT
  USING (
    campaign_id IN (
      SELECT id FROM audit_campaigns
      WHERE auditor_org_id = current_org_id()
         OR client_org_id = current_org_id()
    )
    AND (NOT is_auditor_only OR
      campaign_id IN (
        SELECT id FROM audit_campaigns WHERE auditor_org_id = current_org_id()
      )
    )
    AND NOT is_deleted
  );

CREATE POLICY "Campaign participants can upload"
  ON audit_documents FOR INSERT
  WITH CHECK (
    campaign_id IN (
      SELECT id FROM audit_campaigns
      WHERE auditor_org_id = current_org_id()
         OR client_org_id = current_org_id()
    )
  );

-- CAPA policies
CREATE POLICY "Campaign participants see CAPA"
  ON capa_actions FOR ALL
  USING (
    campaign_id IN (
      SELECT id FROM audit_campaigns
      WHERE auditor_org_id = current_org_id()
         OR client_org_id = current_org_id()
    )
  );

-- Reports policies
CREATE POLICY "Campaign participants see reports"
  ON audit_reports FOR SELECT
  USING (
    campaign_id IN (
      SELECT id FROM audit_campaigns
      WHERE auditor_org_id = current_org_id()
         OR client_org_id = current_org_id()
    )
  );

CREATE POLICY "Auditors create/update reports"
  ON audit_reports FOR ALL
  USING (
    campaign_id IN (
      SELECT id FROM audit_campaigns
      WHERE auditor_org_id = current_org_id()
    )
  );

-- Audit trail policies — READ ONLY for org members, no write via API
CREATE POLICY "Org members see their trail"
  ON audit_trail FOR SELECT
  USING (organization_id = current_org_id());

-- ============================================================
-- DONNÉES INITIALES : Référentiels standards
-- ============================================================
-- Ces templates sont créés en tant que templates "publics" globaux
-- et peuvent être copiés par chaque auditeur.
-- (organization_id = NULL signifie template système)

ALTER TABLE audit_templates ALTER COLUMN organization_id DROP NOT NULL;

-- ============================================================
-- STORAGE BUCKETS (à configurer dans Supabase Dashboard)
-- ============================================================
-- Bucket: audit-documents
--   - Private (accès via signed URLs uniquement)
--   - Max file size: 52428800 (50MB)
--   - Allowed MIME types: application/pdf, image/png, image/jpeg,
--     application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,
--     application/vnd.openxmlformats-officedocument.wordprocessingml.document,
--     application/zip

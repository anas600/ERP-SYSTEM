-- Insert admin user with BCrypt hash for 'Demo1234'
INSERT INTO users (id, email, password_hash, full_name, is_active, two_factor_enabled, is_deleted, created_at, updated_at)
VALUES (
  '11111111-1111-1111-1111-111111111111',
  'admin@alfajr.local',
  '$2a$12$a4NhU5ZG5/73VZDV8eTOEeS2kxzgFqxheg2iWhtJF61kTURu5teDq',
  'System Administrator',
  true,
  false,
  false,
  now(),
  now()
)
ON CONFLICT (email) DO UPDATE
SET password_hash = EXCLUDED.password_hash,
    is_active = true,
    is_deleted = false,
    updated_at = now();

-- Link to holding company
INSERT INTO user_companies (user_id, company_id, is_default, assigned_at)
VALUES (
  '11111111-1111-1111-1111-111111111111',
  '00000000-0000-0000-0000-000000000001',
  true,
  now()
)
ON CONFLICT DO NOTHING;

-- Assign Admin role
INSERT INTO user_roles (user_id, role_id, assigned_at)
SELECT '11111111-1111-1111-1111-111111111111', id, now()
FROM roles
WHERE name = 'Admin'
ON CONFLICT DO NOTHING;

-- Verify
SELECT 'users' AS section, count(*)::text AS n FROM users
UNION ALL SELECT 'user_companies', count(*)::text FROM user_companies
UNION ALL SELECT 'user_roles', count(*)::text FROM user_roles;

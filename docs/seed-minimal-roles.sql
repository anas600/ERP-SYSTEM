-- Create roles
INSERT INTO roles (id, name, description, created_at) VALUES
  (gen_random_uuid(), 'Admin', 'System Administrator (full access)', now()),
  (gen_random_uuid(), 'Accountant', 'Finance staff (AP, AR, reports)', now()),
  (gen_random_uuid(), 'ProjectManager', 'Project manager (projects, tasks, resources)', now()),
  (gen_random_uuid(), 'Viewer', 'Read-only access', now())
ON CONFLICT (name) DO NOTHING;

-- Link admin user to holding company
INSERT INTO user_companies (user_id, company_id, is_default, assigned_at)
SELECT u.id, c.id, true, now()
FROM users u, companies c
WHERE u.email = 'admin@alfajr.local' AND c.is_group = true
ON CONFLICT DO NOTHING;

-- Assign Admin role to admin user
INSERT INTO user_roles (user_id, role_id, assigned_at)
SELECT u.id, r.id, now()
FROM users u, roles r
WHERE u.email = 'admin@alfajr.local' AND r.name = 'Admin'
ON CONFLICT DO NOTHING;

-- Verify
SELECT 'roles' AS section, count(*)::text AS n FROM roles
UNION ALL SELECT 'users', count(*)::text FROM users
UNION ALL SELECT 'user_companies', count(*)::text FROM user_companies
UNION ALL SELECT 'user_roles', count(*)::text FROM user_roles;

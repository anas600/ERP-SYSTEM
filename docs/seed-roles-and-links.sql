-- Insert 4 standard roles
INSERT INTO roles (id, name, description, created_at)
VALUES
  (gen_random_uuid(), 'Admin', 'System Administrator (full access)', now()),
  (gen_random_uuid(), 'Accountant', 'Finance staff (AP, AR, reports)', now()),
  (gen_random_uuid(), 'ProjectManager', 'Project manager (projects, tasks, resources)', now()),
  (gen_random_uuid(), 'Viewer', 'Read-only access', now())
ON CONFLICT (name) DO NOTHING;

-- Link admin user to all 4 roles
INSERT INTO user_roles (user_id, role_id, assigned_at)
SELECT '11111111-1111-1111-1111-111111111111', id, now()
FROM roles
WHERE name IN ('Admin', 'Accountant', 'ProjectManager', 'Viewer')
ON CONFLICT DO NOTHING;

-- Verify
SELECT 'roles' AS t, count(*) AS n FROM roles
UNION ALL SELECT 'user_roles', count(*) FROM user_roles;

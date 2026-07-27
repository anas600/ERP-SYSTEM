CREATE TABLE IF NOT EXISTS public."VersionInfo" (
  "Version" bigint NOT NULL,
  "AppliedOn" timestamp with time zone NOT NULL,
  "Description" varchar(255) NOT NULL
);

INSERT INTO public."VersionInfo" ("Version", "AppliedOn", "Description")
VALUES (20260101000001, now(), 'Phase6_InitialSchema')
ON CONFLICT DO NOTHING;

SELECT 'VersionInfo:' AS section, count(*)::text AS n FROM public."VersionInfo";

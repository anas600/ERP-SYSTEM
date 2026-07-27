CREATE TABLE IF NOT EXISTS "public"."VersionInfo" (
  "Version" bigint NOT NULL,
  "AppliedOn" timestamp with time zone NOT NULL,
  "Description" varchar(255) NOT NULL
);

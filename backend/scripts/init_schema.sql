CREATE EXTENSION IF NOT EXISTS postgis;

CREATE TABLE IF NOT EXISTS "Categories" (
    "Id" uuid PRIMARY KEY,
    "Name" varchar(100) NOT NULL UNIQUE,
    "Description" varchar(500),
    "CreatedAtUtc" timestamptz NOT NULL DEFAULT now(),
    "UpdatedAtUtc" timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS "Users" (
    "Id" uuid PRIMARY KEY,
    "Email" varchar(320) NOT NULL UNIQUE,
    "PasswordHash" varchar(500) NOT NULL,
    "FullName" varchar(200) NOT NULL,
    "Role" varchar(30) NOT NULL DEFAULT 'Citizen',
    "CreatedAtUtc" timestamptz NOT NULL DEFAULT now(),
    "UpdatedAtUtc" timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT "CK_Users_Role" CHECK ("Role" IN ('Citizen', 'Admin', 'FieldTech'))
);

CREATE TABLE IF NOT EXISTS "Issues" (
    "Id" uuid PRIMARY KEY,
    "Title" varchar(200) NOT NULL,
    "Description" varchar(4000) NOT NULL,
    "Status" varchar(30) NOT NULL DEFAULT 'Submitted',
    "Priority" varchar(20) NOT NULL DEFAULT 'Normal',
    "Latitude" double precision NOT NULL,
    "Longitude" double precision NOT NULL,
    "Location" geography(Point, 4326) NOT NULL,
    "CategoryId" uuid NOT NULL REFERENCES "Categories" ("Id"),
    "CreatedByUserId" uuid NOT NULL REFERENCES "Users" ("Id"),
    "AssignedToUserId" uuid REFERENCES "Users" ("Id"),
    "CreatedAtUtc" timestamptz NOT NULL DEFAULT now(),
    "UpdatedAtUtc" timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT "CK_Issues_Status" CHECK ("Status" IN ('Submitted', 'InReview', 'Assigned', 'InProgress', 'Resolved', 'Closed')),
    CONSTRAINT "CK_Issues_Priority" CHECK ("Priority" IN ('Low', 'Normal', 'High', 'Urgent')),
    CONSTRAINT "CK_Issues_Latitude" CHECK ("Latitude" BETWEEN -90 AND 90),
    CONSTRAINT "CK_Issues_Longitude" CHECK ("Longitude" BETWEEN -180 AND 180)
);

CREATE TABLE IF NOT EXISTS "IssueMedia" (
    "Id" uuid PRIMARY KEY,
    "IssueId" uuid NOT NULL REFERENCES "Issues" ("Id") ON DELETE CASCADE,
    "MediaUrl" varchar(2048) NOT NULL,
    "ContentType" varchar(100) NOT NULL,
    "CreatedAtUtc" timestamptz NOT NULL DEFAULT now(),
    "UpdatedAtUtc" timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS "AuditLogs" (
    "Id" uuid PRIMARY KEY,
    "IssueId" uuid REFERENCES "Issues" ("Id") ON DELETE SET NULL,
    "UserId" uuid REFERENCES "Users" ("Id") ON DELETE SET NULL,
    "Action" varchar(100) NOT NULL,
    "Details" varchar(4000),
    "CreatedAtUtc" timestamptz NOT NULL DEFAULT now(),
    "UpdatedAtUtc" timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS "IX_Issues_Status" ON "Issues" ("Status");
CREATE INDEX IF NOT EXISTS "IX_Issues_CategoryId" ON "Issues" ("CategoryId");
CREATE INDEX IF NOT EXISTS "IX_Issues_CreatedByUserId" ON "Issues" ("CreatedByUserId");
CREATE INDEX IF NOT EXISTS "IX_Issues_AssignedToUserId" ON "Issues" ("AssignedToUserId");
CREATE INDEX IF NOT EXISTS "IX_Issues_Location" ON "Issues" USING GIST ("Location");
CREATE INDEX IF NOT EXISTS "IX_IssueMedia_IssueId" ON "IssueMedia" ("IssueId");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_IssueId" ON "AuditLogs" ("IssueId");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_UserId" ON "AuditLogs" ("UserId");

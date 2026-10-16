CREATE TRIGGER "zsync_HouseholdAllocations_insert" AFTER INSERT ON "HouseholdAllocations" WHEN EXISTS(SELECT 1 FROM "SyncBinding" WHERE "Id"=1 AND "Enabled"=1 AND "Applying"=0 AND "TransportKind"=1) BEGIN
INSERT INTO "SyncOutbox"("TableName","RecordKey","Body") VALUES ('HouseholdAllocations',json_array(new."CostId",new."ContactId"),json_object('Amount',new."Amount",'ContactId',new."ContactId",'CostId',new."CostId"));
END;
CREATE TRIGGER "zsync_HouseholdAllocations_update" AFTER UPDATE ON "HouseholdAllocations" WHEN EXISTS(SELECT 1 FROM "SyncBinding" WHERE "Id"=1 AND "Enabled"=1 AND "Applying"=0 AND "TransportKind"=1) AND (old."Amount" IS NOT new."Amount" OR old."ContactId" IS NOT new."ContactId" OR old."CostId" IS NOT new."CostId") BEGIN
INSERT INTO "SyncOutbox"("TableName","RecordKey","Body") SELECT 'HouseholdAllocations',json_array(old."CostId",old."ContactId"),NULL WHERE json_array(old."CostId",old."ContactId") <> json_array(new."CostId",new."ContactId");
INSERT INTO "SyncOutbox"("TableName","RecordKey","Body") VALUES ('HouseholdAllocations',json_array(new."CostId",new."ContactId"),json_object('Amount',new."Amount",'ContactId',new."ContactId",'CostId',new."CostId"));
END;
CREATE TRIGGER "zsync_HouseholdAllocations_delete" AFTER DELETE ON "HouseholdAllocations" WHEN EXISTS(SELECT 1 FROM "SyncBinding" WHERE "Id"=1 AND "Enabled"=1 AND "Applying"=0 AND "TransportKind"=1) BEGIN
INSERT INTO "SyncOutbox"("TableName","RecordKey","Body") VALUES ('HouseholdAllocations',json_array(old."CostId",old."ContactId"),NULL);
END;
CREATE TRIGGER "zsync_HouseholdContacts_insert" AFTER INSERT ON "HouseholdContacts" WHEN EXISTS(SELECT 1 FROM "SyncBinding" WHERE "Id"=1 AND "Enabled"=1 AND "Applying"=0 AND "TransportKind"=1) BEGIN
INSERT INTO "SyncOutbox"("TableName","RecordKey","Body") VALUES ('HouseholdContacts',json_array(new."Id"),json_object('CreatedAt',new."CreatedAt",'Id',new."Id",'IdentityId',new."IdentityId",'IsArchived',new."IsArchived",'Name',new."Name"));
END;
CREATE TRIGGER "zsync_HouseholdContacts_update" AFTER UPDATE ON "HouseholdContacts" WHEN EXISTS(SELECT 1 FROM "SyncBinding" WHERE "Id"=1 AND "Enabled"=1 AND "Applying"=0 AND "TransportKind"=1) AND (old."CreatedAt" IS NOT new."CreatedAt" OR old."Id" IS NOT new."Id" OR old."IdentityId" IS NOT new."IdentityId" OR old."IsArchived" IS NOT new."IsArchived" OR old."Name" IS NOT new."Name") BEGIN
INSERT INTO "SyncOutbox"("TableName","RecordKey","Body") SELECT 'HouseholdContacts',json_array(old."Id"),NULL WHERE json_array(old."Id") <> json_array(new."Id");
INSERT INTO "SyncOutbox"("TableName","RecordKey","Body") VALUES ('HouseholdContacts',json_array(new."Id"),json_object('CreatedAt',new."CreatedAt",'Id',new."Id",'IdentityId',new."IdentityId",'IsArchived',new."IsArchived",'Name',new."Name"));
END;
CREATE TRIGGER "zsync_HouseholdContacts_delete" AFTER DELETE ON "HouseholdContacts" WHEN EXISTS(SELECT 1 FROM "SyncBinding" WHERE "Id"=1 AND "Enabled"=1 AND "Applying"=0 AND "TransportKind"=1) BEGIN
INSERT INTO "SyncOutbox"("TableName","RecordKey","Body") VALUES ('HouseholdContacts',json_array(old."Id"),NULL);
END;
CREATE TRIGGER "zsync_HouseholdCosts_insert" AFTER INSERT ON "HouseholdCosts" WHEN EXISTS(SELECT 1 FROM "SyncBinding" WHERE "Id"=1 AND "Enabled"=1 AND "Applying"=0 AND "TransportKind"=1) BEGIN
INSERT INTO "SyncOutbox"("TableName","RecordKey","Body") VALUES ('HouseholdCosts',json_array(new."Id"),json_object('Amount',new."Amount",'CreatedAt',new."CreatedAt",'CurrencyCode',new."CurrencyCode",'Date',new."Date",'EntryId',new."EntryId",'Id',new."Id",'Kind',new."Kind",'Note',new."Note",'PaidByContactId',new."PaidByContactId",'PaymentKind',new."PaymentKind",'RefundOfCostId',new."RefundOfCostId",'Title',new."Title"));
END;
CREATE TRIGGER "zsync_HouseholdCosts_update" AFTER UPDATE ON "HouseholdCosts" WHEN EXISTS(SELECT 1 FROM "SyncBinding" WHERE "Id"=1 AND "Enabled"=1 AND "Applying"=0 AND "TransportKind"=1) AND (old."Amount" IS NOT new."Amount" OR old."CreatedAt" IS NOT new."CreatedAt" OR old."CurrencyCode" IS NOT new."CurrencyCode" OR old."Date" IS NOT new."Date" OR old."EntryId" IS NOT new."EntryId" OR old."Id" IS NOT new."Id" OR old."Kind" IS NOT new."Kind" OR old."Note" IS NOT new."Note" OR old."PaidByContactId" IS NOT new."PaidByContactId" OR old."PaymentKind" IS NOT new."PaymentKind" OR old."RefundOfCostId" IS NOT new."RefundOfCostId" OR old."Title" IS NOT new."Title") BEGIN
INSERT INTO "SyncOutbox"("TableName","RecordKey","Body") SELECT 'HouseholdCosts',json_array(old."Id"),NULL WHERE json_array(old."Id") <> json_array(new."Id");
INSERT INTO "SyncOutbox"("TableName","RecordKey","Body") VALUES ('HouseholdCosts',json_array(new."Id"),json_object('Amount',new."Amount",'CreatedAt',new."CreatedAt",'CurrencyCode',new."CurrencyCode",'Date',new."Date",'EntryId',new."EntryId",'Id',new."Id",'Kind',new."Kind",'Note',new."Note",'PaidByContactId',new."PaidByContactId",'PaymentKind',new."PaymentKind",'RefundOfCostId',new."RefundOfCostId",'Title',new."Title"));
END;
CREATE TRIGGER "zsync_HouseholdCosts_delete" AFTER DELETE ON "HouseholdCosts" WHEN EXISTS(SELECT 1 FROM "SyncBinding" WHERE "Id"=1 AND "Enabled"=1 AND "Applying"=0 AND "TransportKind"=1) BEGIN
INSERT INTO "SyncOutbox"("TableName","RecordKey","Body") VALUES ('HouseholdCosts',json_array(old."Id"),NULL);
END;
CREATE TRIGGER "zsync_HouseholdSettlements_insert" AFTER INSERT ON "HouseholdSettlements" WHEN EXISTS(SELECT 1 FROM "SyncBinding" WHERE "Id"=1 AND "Enabled"=1 AND "Applying"=0 AND "TransportKind"=1) BEGIN
INSERT INTO "SyncOutbox"("TableName","RecordKey","Body") VALUES ('HouseholdSettlements',json_array(new."Id"),json_object('Amount',new."Amount",'CreatedAt',new."CreatedAt",'CurrencyCode',new."CurrencyCode",'Date',new."Date",'EntryId',new."EntryId",'FromContactId',new."FromContactId",'Id',new."Id",'Note',new."Note",'ToContactId',new."ToContactId",'UsesDestinationAmount',new."UsesDestinationAmount"));
END;
CREATE TRIGGER "zsync_HouseholdSettlements_update" AFTER UPDATE ON "HouseholdSettlements" WHEN EXISTS(SELECT 1 FROM "SyncBinding" WHERE "Id"=1 AND "Enabled"=1 AND "Applying"=0 AND "TransportKind"=1) AND (old."Amount" IS NOT new."Amount" OR old."CreatedAt" IS NOT new."CreatedAt" OR old."CurrencyCode" IS NOT new."CurrencyCode" OR old."Date" IS NOT new."Date" OR old."EntryId" IS NOT new."EntryId" OR old."FromContactId" IS NOT new."FromContactId" OR old."Id" IS NOT new."Id" OR old."Note" IS NOT new."Note" OR old."ToContactId" IS NOT new."ToContactId" OR old."UsesDestinationAmount" IS NOT new."UsesDestinationAmount") BEGIN
INSERT INTO "SyncOutbox"("TableName","RecordKey","Body") SELECT 'HouseholdSettlements',json_array(old."Id"),NULL WHERE json_array(old."Id") <> json_array(new."Id");
INSERT INTO "SyncOutbox"("TableName","RecordKey","Body") VALUES ('HouseholdSettlements',json_array(new."Id"),json_object('Amount',new."Amount",'CreatedAt',new."CreatedAt",'CurrencyCode',new."CurrencyCode",'Date',new."Date",'EntryId',new."EntryId",'FromContactId',new."FromContactId",'Id',new."Id",'Note',new."Note",'ToContactId',new."ToContactId",'UsesDestinationAmount',new."UsesDestinationAmount"));
END;
CREATE TRIGGER "zsync_HouseholdSettlements_delete" AFTER DELETE ON "HouseholdSettlements" WHEN EXISTS(SELECT 1 FROM "SyncBinding" WHERE "Id"=1 AND "Enabled"=1 AND "Applying"=0 AND "TransportKind"=1) BEGIN
INSERT INTO "SyncOutbox"("TableName","RecordKey","Body") VALUES ('HouseholdSettlements',json_array(old."Id"),NULL);
END;

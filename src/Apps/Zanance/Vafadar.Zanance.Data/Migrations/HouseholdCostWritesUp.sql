CREATE TRIGGER "zhh_guard_HouseholdAllocations_insert" BEFORE INSERT ON "HouseholdAllocations" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('HouseholdAllocations','insert',json_array(NEW."CostId",NEW."ContactId"));
END;
CREATE TRIGGER "zhh_guard_HouseholdAllocations_update" BEFORE UPDATE ON "HouseholdAllocations" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('HouseholdAllocations','update',json_array(NEW."CostId",NEW."ContactId"));
END;
CREATE TRIGGER "zhh_guard_HouseholdAllocations_delete" BEFORE DELETE ON "HouseholdAllocations" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('HouseholdAllocations','delete',json_array(OLD."CostId",OLD."ContactId"));
END;
CREATE TRIGGER "zhh_guard_HouseholdContacts_insert" BEFORE INSERT ON "HouseholdContacts" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('HouseholdContacts','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_HouseholdContacts_update" BEFORE UPDATE ON "HouseholdContacts" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('HouseholdContacts','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_HouseholdContacts_delete" BEFORE DELETE ON "HouseholdContacts" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('HouseholdContacts','delete',json_array(OLD."Id"));
END;
CREATE TRIGGER "zhh_guard_HouseholdCosts_insert" BEFORE INSERT ON "HouseholdCosts" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('HouseholdCosts','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_HouseholdCosts_update" BEFORE UPDATE ON "HouseholdCosts" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('HouseholdCosts','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_HouseholdCosts_delete" BEFORE DELETE ON "HouseholdCosts" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('HouseholdCosts','delete',json_array(OLD."Id"));
END;
CREATE TRIGGER "zhh_guard_HouseholdSettlements_insert" BEFORE INSERT ON "HouseholdSettlements" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('HouseholdSettlements','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_HouseholdSettlements_update" BEFORE UPDATE ON "HouseholdSettlements" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('HouseholdSettlements','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_HouseholdSettlements_delete" BEFORE DELETE ON "HouseholdSettlements" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('HouseholdSettlements','delete',json_array(OLD."Id"));
END;

-- Fixed v1 financial mutation boundary; household membership is independent of commercial test switches.
CREATE TRIGGER "zhh_guard_Accounts_insert" BEFORE INSERT ON "Accounts" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Accounts','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_Accounts_update" BEFORE UPDATE ON "Accounts" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Accounts','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_Accounts_delete" BEFORE DELETE ON "Accounts" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Accounts','delete',json_array(OLD."Id"));
END;
CREATE TRIGGER "zhh_guard_AssetEvents_insert" BEFORE INSERT ON "AssetEvents" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('AssetEvents','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_AssetEvents_update" BEFORE UPDATE ON "AssetEvents" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('AssetEvents','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_AssetEvents_delete" BEFORE DELETE ON "AssetEvents" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('AssetEvents','delete',json_array(OLD."Id"));
END;
CREATE TRIGGER "zhh_guard_AssetLocations_insert" BEFORE INSERT ON "AssetLocations" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('AssetLocations','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_AssetLocations_update" BEFORE UPDATE ON "AssetLocations" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('AssetLocations','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_AssetLocations_delete" BEFORE DELETE ON "AssetLocations" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('AssetLocations','delete',json_array(OLD."Id"));
END;
CREATE TRIGGER "zhh_guard_AssetTypes_insert" BEFORE INSERT ON "AssetTypes" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('AssetTypes','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_AssetTypes_update" BEFORE UPDATE ON "AssetTypes" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('AssetTypes','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_AssetTypes_delete" BEFORE DELETE ON "AssetTypes" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('AssetTypes','delete',json_array(OLD."Id"));
END;
CREATE TRIGGER "zhh_guard_AssetValuations_insert" BEFORE INSERT ON "AssetValuations" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('AssetValuations','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_AssetValuations_update" BEFORE UPDATE ON "AssetValuations" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('AssetValuations','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_AssetValuations_delete" BEFORE DELETE ON "AssetValuations" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('AssetValuations','delete',json_array(OLD."Id"));
END;
CREATE TRIGGER "zhh_guard_Attachments_insert" BEFORE INSERT ON "Attachments" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Attachments','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_Attachments_update" BEFORE UPDATE ON "Attachments" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Attachments','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_Attachments_delete" BEFORE DELETE ON "Attachments" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Attachments','delete',json_array(OLD."Id"));
END;
CREATE TRIGGER "zhh_guard_BudgetCategoryLimits_insert" BEFORE INSERT ON "BudgetCategoryLimits" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('BudgetCategoryLimits','insert',json_array(NEW."BudgetId",NEW."CategoryId"));
END;
CREATE TRIGGER "zhh_guard_BudgetCategoryLimits_update" BEFORE UPDATE ON "BudgetCategoryLimits" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('BudgetCategoryLimits','update',json_array(NEW."BudgetId",NEW."CategoryId"));
END;
CREATE TRIGGER "zhh_guard_BudgetCategoryLimits_delete" BEFORE DELETE ON "BudgetCategoryLimits" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('BudgetCategoryLimits','delete',json_array(OLD."BudgetId",OLD."CategoryId"));
END;
CREATE TRIGGER "zhh_guard_Budgets_insert" BEFORE INSERT ON "Budgets" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Budgets','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_Budgets_update" BEFORE UPDATE ON "Budgets" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Budgets','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_Budgets_delete" BEFORE DELETE ON "Budgets" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Budgets','delete',json_array(OLD."Id"));
END;
CREATE TRIGGER "zhh_guard_Categories_insert" BEFORE INSERT ON "Categories" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Categories','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_Categories_update" BEFORE UPDATE ON "Categories" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Categories','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_Categories_delete" BEFORE DELETE ON "Categories" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Categories','delete',json_array(OLD."Id"));
END;
CREATE TRIGGER "zhh_guard_CategoryRules_insert" BEFORE INSERT ON "CategoryRules" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('CategoryRules','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_CategoryRules_update" BEFORE UPDATE ON "CategoryRules" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('CategoryRules','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_CategoryRules_delete" BEFORE DELETE ON "CategoryRules" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('CategoryRules','delete',json_array(OLD."Id"));
END;
CREATE TRIGGER "zhh_guard_ContributionPlans_insert" BEFORE INSERT ON "ContributionPlans" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('ContributionPlans','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_ContributionPlans_update" BEFORE UPDATE ON "ContributionPlans" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('ContributionPlans','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_ContributionPlans_delete" BEFORE DELETE ON "ContributionPlans" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('ContributionPlans','delete',json_array(OLD."Id"));
END;
CREATE TRIGGER "zhh_guard_Entries_insert" BEFORE INSERT ON "Entries" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Entries','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_Entries_update" BEFORE UPDATE ON "Entries" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Entries','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_Entries_delete" BEFORE DELETE ON "Entries" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Entries','delete',json_array(OLD."Id"));
END;
CREATE TRIGGER "zhh_guard_ExchangeRates_insert" BEFORE INSERT ON "ExchangeRates" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('ExchangeRates','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_ExchangeRates_update" BEFORE UPDATE ON "ExchangeRates" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('ExchangeRates','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_ExchangeRates_delete" BEFORE DELETE ON "ExchangeRates" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('ExchangeRates','delete',json_array(OLD."Id"));
END;
CREATE TRIGGER "zhh_guard_ForecastSnapshots_insert" BEFORE INSERT ON "ForecastSnapshots" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('ForecastSnapshots','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_ForecastSnapshots_update" BEFORE UPDATE ON "ForecastSnapshots" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('ForecastSnapshots','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_ForecastSnapshots_delete" BEFORE DELETE ON "ForecastSnapshots" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('ForecastSnapshots','delete',json_array(OLD."Id"));
END;
CREATE TRIGGER "zhh_guard_GoalAllocations_insert" BEFORE INSERT ON "GoalAllocations" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('GoalAllocations','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_GoalAllocations_update" BEFORE UPDATE ON "GoalAllocations" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('GoalAllocations','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_GoalAllocations_delete" BEFORE DELETE ON "GoalAllocations" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('GoalAllocations','delete',json_array(OLD."Id"));
END;
CREATE TRIGGER "zhh_guard_Goals_insert" BEFORE INSERT ON "Goals" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Goals','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_Goals_update" BEFORE UPDATE ON "Goals" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Goals','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_Goals_delete" BEFORE DELETE ON "Goals" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Goals','delete',json_array(OLD."Id"));
END;
CREATE TRIGGER "zhh_guard_OccurrenceStates_insert" BEFORE INSERT ON "OccurrenceStates" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('OccurrenceStates','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_OccurrenceStates_update" BEFORE UPDATE ON "OccurrenceStates" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('OccurrenceStates','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_OccurrenceStates_delete" BEFORE DELETE ON "OccurrenceStates" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('OccurrenceStates','delete',json_array(OLD."Id"));
END;
CREATE TRIGGER "zhh_guard_SavedFilters_insert" BEFORE INSERT ON "SavedFilters" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('SavedFilters','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_SavedFilters_update" BEFORE UPDATE ON "SavedFilters" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('SavedFilters','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_SavedFilters_delete" BEFORE DELETE ON "SavedFilters" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('SavedFilters','delete',json_array(OLD."Id"));
END;
CREATE TRIGGER "zhh_guard_Schedules_insert" BEFORE INSERT ON "Schedules" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Schedules','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_Schedules_update" BEFORE UPDATE ON "Schedules" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Schedules','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_Schedules_delete" BEFORE DELETE ON "Schedules" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Schedules','delete',json_array(OLD."Id"));
END;
CREATE TRIGGER "zhh_guard_Settings_insert" BEFORE INSERT ON "Settings" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Settings','insert',json_array());
END;
CREATE TRIGGER "zhh_guard_Settings_update" BEFORE UPDATE ON "Settings" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) AND (OLD."BudgetCalendar" IS NOT NEW."BudgetCalendar" OR OLD."DefaultAccountId" IS NOT NEW."DefaultAccountId" OR OLD."DefaultCurrencyCode" IS NOT NEW."DefaultCurrencyCode" OR OLD."DisplayUnits" IS NOT NEW."DisplayUnits" OR OLD."EssentialEstimate" IS NOT NEW."EssentialEstimate" OR OLD."EssentialEstimateCurrency" IS NOT NEW."EssentialEstimateCurrency" OR OLD."EssentialEstimatePeriod" IS NOT NEW."EssentialEstimatePeriod" OR OLD."FortnightStart" IS NOT NEW."FortnightStart" OR OLD."HomeBudgetCurrencyCode" IS NOT NEW."HomeBudgetCurrencyCode" OR OLD."HomeLayout" IS NOT NEW."HomeLayout" OR OLD."Mode" IS NOT NEW."Mode" OR OLD."MonthStartDay" IS NOT NEW."MonthStartDay" OR OLD."RateFreshnessDays" IS NOT NEW."RateFreshnessDays" OR OLD."ReportCurrencyCode" IS NOT NEW."ReportCurrencyCode" OR OLD."ReviewProgress" IS NOT NEW."ReviewProgress" OR OLD."ValuationCurrencyEnabled" IS NOT NEW."ValuationCurrencyEnabled") BEGIN
    SELECT zanance_household_write('Settings','update',json_array());
END;
CREATE TRIGGER "zhh_guard_Settings_delete" BEFORE DELETE ON "Settings" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Settings','delete',json_array());
END;
CREATE TRIGGER "zhh_guard_Templates_insert" BEFORE INSERT ON "Templates" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Templates','insert',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_Templates_update" BEFORE UPDATE ON "Templates" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Templates','update',json_array(NEW."Id"));
END;
CREATE TRIGGER "zhh_guard_Templates_delete" BEFORE DELETE ON "Templates" WHEN (EXISTS(SELECT 1 FROM "HouseholdLocalBinding") OR EXISTS(SELECT 1 FROM "SyncBinding" WHERE "TransportKind"=1)) BEGIN
    SELECT zanance_household_write('Templates','delete',json_array(OLD."Id"));
END;

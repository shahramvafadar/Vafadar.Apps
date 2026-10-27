import android.os.Build;
import android.os.IBinder;
import android.os.Process;
import android.util.JsonWriter;
import java.io.OutputStreamWriter;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.nio.charset.StandardCharsets;

/** Read-only shell query of the real framework; never a backup, restore or policy setter. */
public final class QueryBackupEligibility {
    private static final String MAIN = "pro.vafadar.zanance";
    private static final String CALLER = MAIN + ".androidhomereview";
    private static final String CONTROL = "com.android.providers.settings";

    /** No instance or application lifecycle is created for this shell-only observer. */
    private QueryBackupEligibility() { }

    /** Binds the two exact owned UIDs before querying only fixed read-only service methods. */
    public static void main(String[] args) throws Exception {
        try {
            if (args.length != 3 || Process.myUid() != 2000 || Build.VERSION.SDK_INT != 36)
                throw new IllegalArgumentException("Exact shell/API36 admission required");
            boolean emulator = args[0].equals("emulator");
            if (!emulator && !args[0].equals("phone")) throw new IllegalArgumentException("Explicit mode required");
            if (emulator != Build.HARDWARE.equals("ranchu")
                    || !emulator && !Build.MODEL.equals("SM-A176B"))
                throw new IllegalArgumentException("Exact owned device class required");
            int mainUid = Integer.parseInt(args[1]);
            int callerUid = Integer.parseInt(args[2]);
            if (mainUid < 10000 || callerUid < 10000 || mainUid == callerUid)
                throw new IllegalArgumentException("Distinct original application UIDs required");

            Object packages = Class.forName("android.app.AppGlobals")
                    .getMethod("getPackageManager").invoke(null);
            Method uid = Class.forName("android.content.pm.IPackageManager")
                    .getMethod("getPackageUid", String.class, long.class, int.class);
            String[] names = emulator ? new String[] { MAIN, CALLER, CONTROL }
                                      : new String[] { MAIN, CALLER };
            int[] expected = emulator ? new int[] { mainUid, callerUid, 1000 }
                                     : new int[] { mainUid, callerUid };
            for (int i = 0; i < names.length; i++) {
                if (((Integer) uid.invoke(packages, names[i], 0L, 0)).intValue() != expected[i])
                    throw new IllegalArgumentException("Original package UID differs");
            }

            // Resolve typed names instead of guessing Binder transaction numbers.
            IBinder binder = (IBinder) Class.forName("android.os.ServiceManager")
                    .getMethod("getService", String.class).invoke(null, "backup");
            if (binder == null) throw new IllegalStateException("Backup service absent");
            Object backup = Class.forName("android.app.backup.IBackupManager$Stub")
                    .getMethod("asInterface", IBinder.class).invoke(null, binder);
            Class<?> contract = Class.forName("android.app.backup.IBackupManager");
            boolean active = (Boolean) contract.getMethod("isBackupServiceActive", int.class)
                    .invoke(backup, 0);
            boolean enabled = (Boolean) contract.getMethod("isBackupEnabledForUser", int.class)
                    .invoke(backup, 0);
            Method eligible = contract.getMethod("isAppEligibleForBackupForUser", int.class, String.class);
            boolean[] results = new boolean[names.length];
            for (int i = 0; i < names.length; i++) results[i] = (Boolean) eligible.invoke(backup, 0, names[i]);

            JsonWriter output = writer();
            output.beginObject().name("Mode").value(args[0]).name("ActorUid").value(Process.myUid())
                    .name("Api").value(Build.VERSION.SDK_INT).name("Model").value(Build.MODEL)
                    .name("User").value(0).name("BackupServiceActive").value(active)
                    .name("BackupEnabled").value(enabled).name("Packages").beginArray();
            for (int i = 0; i < names.length; i++) {
                output.beginObject().name("Package").value(names[i]).name("Uid").value(expected[i])
                        .name("ReportedEligible").value(results[i]).endObject();
            }
            output.endArray().name("BackupRestoreOrSetterRequested").value(false)
                    .name("PrivateDatabaseKeyOrAccountRead").value(false).endObject();
            output.flush();
        } catch (Throwable error) {
            // A denied/unsupported query is an error, never a synthetic ineligible result.
            Throwable cause = error instanceof InvocationTargetException
                    && error.getCause() != null ? error.getCause() : error;
            JsonWriter output = writer();
            output.beginObject().name("ErrorType").value(cause.getClass().getName())
                    .name("NoEligibilityResultAccepted").value(true).endObject();
            output.flush();
            System.exit(3);
        }
    }

    /** Emits only the fixed public policy metadata, never an exception message or stack. */
    private static JsonWriter writer() {
        return new JsonWriter(new OutputStreamWriter(System.out, StandardCharsets.UTF_8));
    }
}

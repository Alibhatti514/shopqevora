using System.Diagnostics;
using MySqlConnector;
namespace ShopQevora.Maintenance;

// Explicit operator action for an existing EnsureCreated database. No startup DDL.
public static class ContactUpgrade
{
    public static async Task Apply(string source,string sqlPath)
    {
        if(OperatingSystem.IsWindows())throw new InvalidOperationException("Use a reviewed private backup and the contact SQL upgrade manually on Windows.");
        var settings=new MySqlConnectionStringBuilder(source);
        if(settings.Server is not ("localhost" or "127.0.0.1" or "::1"))throw new InvalidOperationException("Automatic upgrades are restricted to local MySQL.");
        await using var connection=new MySqlConnection(source);await connection.OpenAsync();
        async Task<object?> Scalar(string sql){using var command=connection.CreateCommand();command.CommandText=sql;return await command.ExecuteScalarAsync();}
        if(Convert.ToInt32(await Scalar("SELECT GET_LOCK('shopqevora_contact_schema',15)"))!=1)throw new InvalidOperationException("Another contact upgrade is active.");
        try
        {
            if(Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema=DATABASE() AND table_name='ContactInquiries'"))==1)
            {
                var columns=Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='ContactInquiries' AND column_name IN ('Id','Reference','SubmissionKeyHash','RequestHash','FullName','Email','Phone','Category','OrderReference','Message','UserId','CreatedUtc')"));
                var indexes=Convert.ToInt32(await Scalar("SELECT COUNT(DISTINCT index_name) FROM information_schema.statistics WHERE table_schema=DATABASE() AND table_name='ContactInquiries' AND non_unique=0 AND column_name IN ('Id','Reference','SubmissionKeyHash')"));
                if(columns!=12||indexes!=3)throw new InvalidOperationException("Existing contact schema differs from the reviewed contract; review manually. No changes made.");
                Console.WriteLine("ContactInquiries already has the expected columns and unique indexes; no changes applied.");return;
            }
            var tables=new[]{"AspNetUsers","Products","CartItems","Orders","OrderLines","GuestCheckouts","CustomerProfiles"};
            var before=new Dictionary<string,long>();
            foreach(var table in tables)before[table]=Convert.ToInt64(await Scalar($"SELECT COUNT(*) FROM `{table}`"));
            var directory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".local/share/ShopQevora/backups");
            Directory.CreateDirectory(directory);File.SetUnixFileMode(directory,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
            var backup=Path.Combine(directory,"before-contact-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss")+".sql");
            using(var output=new FileStream(backup,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {
                File.SetUnixFileMode(backup,UnixFileMode.UserRead|UnixFileMode.UserWrite);
                var start=new ProcessStartInfo("mysqldump"){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
                foreach(var value in new[]{"--host="+settings.Server,"--port="+settings.Port,"--user="+settings.UserID,"--single-transaction","--no-tablespaces","--skip-lock-tables",settings.Database})start.ArgumentList.Add(value);
                start.Environment["MYSQL_PWD"]=settings.Password;
                using var process=Process.Start(start)!;var errors=process.StandardError.ReadToEndAsync();
                await process.StandardOutput.BaseStream.CopyToAsync(output);await process.WaitForExitAsync();await errors;
                if(process.ExitCode!=0||output.Length<1024)throw new InvalidOperationException("The private backup failed. No schema upgrade was applied.");
            }
            using(var command=connection.CreateCommand()){command.CommandText=await File.ReadAllTextAsync(sqlPath);await command.ExecuteNonQueryAsync();}
            foreach(var table in tables)if(before[table]!=Convert.ToInt64(await Scalar($"SELECT COUNT(*) FROM `{table}`")))throw new InvalidOperationException("An existing row count changed concurrently; inspect privately. This upgrade executed only CREATE TABLE.");
            Console.WriteLine("Private full backup completed outside the repository. Added ContactInquiries only; all existing table row counts are unchanged.");
        }
        finally{await Scalar("SELECT RELEASE_LOCK('shopqevora_contact_schema')");}
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations.Install;
using Umbraco.Web.Install;
using Umbraco.Web.Install.InstallSteps;
using Umbraco.Web.Install.Models;
using Umbraco.Web.Migrations.PostMigrations;

namespace Umbraco.Migration.Contrib
{
    [InstallSetupStep(InstallationType.Upgrade | InstallationType.NewInstall,
    "DatabaseUpgradeWithContrib", 12, "")]
    internal class DatabaseUpgradeStep : Umbraco.Web.Install.InstallSteps.DatabaseUpgradeStep
    {
        private readonly DatabaseBuilder _databaseBuilder;
        private readonly ILogger _logger;

        public DatabaseUpgradeStep(DatabaseBuilder databaseBuilder, IRuntimeState runtime, ILogger logger) : base(databaseBuilder, runtime, logger)
        {
            _databaseBuilder = databaseBuilder;
            _logger = logger;            
        }

        public override Task<InstallSetupResult> ExecuteAsync(object model)
        {
            var installSteps = InstallStatusTracker.GetStatus().ToArray();
            var previousStep = installSteps.Single(x => x.Name == "DatabaseInstall");
            var upgrade = previousStep.AdditionalData.ContainsKey("upgrade");

            if (upgrade)
            {
                var plan = new ModifiedPlan();
                plan.AddPostMigration<ClearCsrfCookies>(); // needed when running installer (back-office)

                var result = _databaseBuilder.UpgradeSchemaAndData(plan);

                if (result.Success == false)
                {
                    throw new InstallException("The database failed to upgrade. ERROR: " + result.Message);
                }

                DatabaseInstallStep.HandleConnectionStrings(_logger);
            }

            return Task.FromResult<InstallSetupResult>(null);
        }
    }
}

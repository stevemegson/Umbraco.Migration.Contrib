using LightInject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Composing;

namespace Umbraco.Migration.Contrib
{
    public class Composer : IComposer
    {
        public void Compose(Composition composition)
        {
            if (composition.RuntimeState.Level == Core.RuntimeLevel.Upgrade)
            {
                // Replace Umbraco.Web.Install.InstallSteps.DatabaseUpgradeStep with our own DatabaseUpgradeStep
                
                (composition.Concrete as ServiceContainer)
                    .Override(r => r.ServiceType == typeof(Umbraco.Web.Install.InstallSteps.DatabaseUpgradeStep),
                    (f, r) => new ServiceRegistration()
                    {
                        ServiceType = typeof(Umbraco.Web.Install.InstallSteps.DatabaseUpgradeStep),
                        ImplementingType = typeof(DatabaseUpgradeStep),
                        Lifetime = null,
                        ServiceName = ""
                    });
            }
        }
    }
}

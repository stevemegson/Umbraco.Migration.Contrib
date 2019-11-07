using LightInject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Migrations.Upgrade;

namespace Umbraco.Migration.Contrib
{
    public class Composer : IComposer
    {
        public void Compose(Composition composition)
        {
            if (composition.RuntimeState.Level == Core.RuntimeLevel.Upgrade)
            {
                composition.RegisterUnique<UmbracoPlan, ModifiedPlan>();
            }
        }
    }
}

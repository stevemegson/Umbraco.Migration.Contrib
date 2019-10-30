using Semver;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Migrations;
using Umbraco.Core.Migrations.Upgrade;
using Umbraco.Core.Migrations.Upgrade.Common;
using Umbraco.Core.Migrations.Upgrade.V_8_0_0;
using Umbraco.Core.Migrations.Upgrade.V_8_0_1;
using Umbraco.Core.Migrations.Upgrade.V_8_1_0;
using Umbraco.Migration.Contrib.Migrations;

namespace Umbraco.Migration.Contrib
{
    public class ModifiedPlan : UmbracoPlan
    {
        private Type _insertBefore;
        private HashSet<string> _modifiedStates = new HashSet<string>();

        public ModifiedPlan()
        {            
            ModifyPlan();

#if DEBUG
            ListTransitions();
#endif
        }

        protected void ModifyPlan()
        {
            Before<PropertyEditorsMigration>();
            Insert<NestedContentPropertyEditors>();
            Insert<LegacyPickersPropertyEditors>();

            Before<CreateKeysAndIndexes>();
            Insert<DeletePropertyDataIndexes>();

            Replace<VariantsMigration, ModifiedVariantsMigration>();
            Replace<ConvertTinyMceAndGridMediaUrlsToLocalLink, ModifiedConvertTinyMceAndGridMediaUrlsToLocalLink>();
            Replace<RenameMediaVersionTable, ModifiedRenameMediaVersionTable>();
            Replace<PropertyEditorsMigration, ModifiedPropertyEditorsMigration>();
        }

        protected MigrationPlan Replace<TExisting, TMigration>()
        {
            var transitions = (Transitions as Dictionary<string, Transition>);
            foreach(var t in transitions.Values.ToArray().Where(t => t?.MigrationType == typeof(TExisting)))
            {                
                transitions[t.SourceState] = new Transition(t.SourceState, t.TargetState, typeof(TMigration));
                _modifiedStates.Add(t.SourceState);
            }
            
            return this;
        }

        protected MigrationPlan Before<TMigration>()
        {
            _insertBefore = typeof(TMigration);

            return this;
        }

        protected MigrationPlan Insert<TMigration>()
        {
            var transitions = (Transitions as Dictionary<string, Transition>);
            foreach (var t in transitions.Values.ToArray().Where(t => t?.MigrationType == _insertBefore))
            {
                var interimState = CreateRandomState();
                transitions[t.SourceState] = new Transition(t.SourceState, interimState, typeof(TMigration));
                transitions[interimState] = new Transition(interimState, t.TargetState, _insertBefore);

                _modifiedStates.Add(t.SourceState);                
            }

            return this;
        }

        private void ListTransitions()
        {            
            foreach (var t in Transitions)
            {
                bool modified = _modifiedStates.Contains(t.Key);
                string color = modified ? "color=red fontcolor=red" : "";

                if (t.Value == null)
                {
                    System.Diagnostics.Debug.WriteLine($"\"{t.Key}\" -> \"Final\" [{color}]");
                }
                else if (t.Value.MigrationType == typeof(NoopMigration))
                    System.Diagnostics.Debug.WriteLine($"\"{t.Value.SourceState}\" -> \"{t.Value.TargetState}\" [{color}]");
                else
                    System.Diagnostics.Debug.WriteLine($"\"{t.Value.SourceState}\" -> \"{t.Value.TargetState}\" [{color} label=\"{t.Value.MigrationType.Name}\"]");
            }
        }
    }
}
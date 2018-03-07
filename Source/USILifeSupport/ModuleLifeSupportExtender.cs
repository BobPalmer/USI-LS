using System;
using System.Text;
using System.Collections.Generic;
using USITools;

namespace LifeSupport
{
    public class ModuleLifeSupportExtender : ModuleResourceConverter_USI
    {
        [KSPField]
        public float TimeMultiplier= 1f;

        [KSPField]
        public bool PartOnly = false;

        [KSPField]
        public string restrictedClass = "";

        [KSPField]
        public bool homeTimer = true;

        [KSPField]
        public bool habTimer = true;

        [KSPField(guiName = "Kolony Growth", guiActive = true, guiActiveEditor = true, isPersistant = true), UI_Toggle(disabledText = "disabled", enabledText = "enabled")]
        public bool KolonyGrowthEnabled = false;

        [KSPField(isPersistant = true)]
        public double GrowthTime = 0d;

        public const double GestationTime = 9720000d;

        protected override void PreProcessing()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            base.PreProcessing();
            var v = LifeSupportManager.Instance.FetchVessel(vessel.id.ToString());
            var e = 1d;
            if (v != null)
            {
                e += v.VesselHabMultiplier;
            }
            EfficiencyBonus = (float)e;
        }


        protected override void PostProcess(ConverterResults result, double deltaTime)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            base.PostProcess(result, deltaTime);
            var baseTime = TimeMultiplier*result.TimeFactor;
            var kerbals = new List<ProtoCrewMember>();
            var crew = vessel.GetVesselCrew();
            if (PartOnly)
                crew = part.protoModuleCrew;

            var hasMale = false;
            var hasFemale = false;

            var count = crew.Count;
            for (int i = 0; i < count; ++i)
            {
                var c = crew[i];
                if (c.gender == ProtoCrewMember.Gender.Male)
                    hasMale = true;
                if (c.gender == ProtoCrewMember.Gender.Female)
                    hasFemale = true;

                if (string.IsNullOrEmpty(restrictedClass) || c.experienceTrait.Title == restrictedClass)
                    kerbals.Add(c);
            }

            if (kerbals.Count == 0)
                return;

            var timePerKerbal = baseTime/kerbals.Count;

            count = kerbals.Count;
            for(int i = 0; i < count; ++i)
            {
                var k = kerbals[i];
                var lsKerbal = LifeSupportManager.Instance.FetchKerbal(k);
                if(homeTimer)
                    lsKerbal.MaxOffKerbinTime += timePerKerbal;
                if(habTimer)
                    lsKerbal.TimeEnteredVessel += timePerKerbal;

                LifeSupportManager.Instance.TrackKerbal(lsKerbal);
            }

            //Kolony Growth
            if (KolonyGrowthEnabled && part.CrewCapacity > part.protoModuleCrew.Count && hasMale && hasFemale)
            {
                GrowthTime += (result.TimeFactor * part.protoModuleCrew.Count);
                if (GrowthTime >= GestationTime)
                {
                    GrowthTime -= GestationTime;
                    SpawnKerbal();
                }
            }
        }

        private void SpawnKerbal()
        {
            ProtoCrewMember newKerb = HighLogic.CurrentGame.CrewRoster.GetNewKerbal();
            newKerb.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
            part.AddCrewmember(newKerb);
            var msg = String.Format("{0}, a new {1}, has joined your crew!", newKerb.name, newKerb.experienceTrait.TypeName);
            ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        public override string GetInfo()
        {
            var output = new StringBuilder("");
            output.Append(base.GetInfo());
            output.Append(Environment.NewLine);
            output.Append("Pushes back onboard kerbals hab/home timers");
            output.Append(Environment.NewLine);
            output.AppendFormat("Rated for: {0} kerbals", TimeMultiplier);
            output.Append(Environment.NewLine);
            if (PartOnly)
            {
                output.Append("Effects only kerbals in this part");
                output.Append(Environment.NewLine);
            }
            if (!String.IsNullOrEmpty(restrictedClass))
            {
                output.AppendFormat("Effects only {0}s", restrictedClass);
                output.Append(Environment.NewLine);
            }
            return output.ToString();
        }


    }
}
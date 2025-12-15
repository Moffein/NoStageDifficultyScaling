using RoR2;
using UnityEngine;
using System.Security.Permissions;
using System.Security;
using BepInEx;
using MonoMod.Cil;
using BepInEx.Configuration;
using System;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ScalingExperiment
{
    [BepInPlugin("com.Moffein.NoStageDifficultyScaling", "No Stage Difficulty Scaling", "1.0.0")]
    public class NoStageDifficultyScaling : BaseUnityPlugin
    {
        public static ConfigEntry<float> stageDurationBaseline;

        internal void Awake()
        {
            stageDurationBaseline = base.Config.Bind < float>(new ConfigDefinition("Settings", "Stage Clear Time"), 360f, new ConfigDescription("Expected time in seconds to clear a stage."));
            if (stageDurationBaseline.Value <= 0f)
            {
                Debug.LogError("NoStageDifficultyScaling: Baseline stage clear timer must be greater than 0.");
                return;
            }
            IL.RoR2.Run.RecalculateDifficultyCoefficentInternal += Run_RecalculateDifficultyCoefficentInternal;
        }

        private static void Run_RecalculateDifficultyCoefficentInternal(MonoMod.Cil.ILContext il)
        {
            bool isError = true;
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, x => x.MatchLdfld<Run>("stageClearCount"), x => x.MatchConvR4()))
            {
                c.EmitDelegate<Func<float, float>>(GetTimeScaledStages);

                if (c.TryGotoNext(MoveType.After, x => x.MatchLdfld<Run>("stageClearCount"), x => x.MatchConvR4()))
                {
                    c.EmitDelegate<Func<float, float>>(GetTimeScaledStages);
                    isError = false;
                }
            }

            if (isError)
            {
                Debug.LogError("ScalingExperiment: Run_RecalculateDifficultyCoefficentInternal IL hook failed.");
            }
        }

        public static float GetTimeScaledStages(float stages)
        {
            if (Run.instance)
            {
                return Run.instance.GetRunStopwatch() / stageDurationBaseline.Value;
            }
            return stages;
        }
    }
}

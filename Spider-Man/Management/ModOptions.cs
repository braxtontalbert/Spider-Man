using System;
using ThunderRoad;

namespace Spider_Man.Management
{

    enum SpellOption
    {
        Gaze,
        Point
    }
    public class ModOptions : ThunderScript
    {
        public static int quality;
        public static float damper;
        public static float strength;
        public static float velocity;
        public static float waveCount;
        public static float waveHeight;
        public static int rotation;
        public static int reelInPower;
        public static int reelOutPower;
        public static int webZipPower;
        public static int jumpPower;

        public static float alignmentSpeed;

        public static ModOptionInt[] intOption = new ModOptionInt[1001];
        public static ModOptionInt[] reelOption = new ModOptionInt[21];
        public static ModOptionInt[] webZipPowerOption = new ModOptionInt[1001];
        public static ModOptionInt[] rotationOption = new ModOptionInt[361];
        public static ModOptionFloat[] floatOption = new ModOptionFloat[101];
        public static ModOptionFloat[] strengthOption = new ModOptionFloat[1601];
        public static ModOptionFloat[] waveOption = new ModOptionFloat[30];
        public static ModOptionInt[] jumpPowerOption = new ModOptionInt[11];
        public static ModOptionBool[] booleanOptions = new ModOptionBool[2]
        {
            new ModOptionBool("Disabled", false),
            new ModOptionBool("Enabled",true)
        };
        public static ModOptionString[] weblineOptions = new ModOptionString[2]
        {
            new ModOptionString("Classic", "Classic"),
            new ModOptionString("Realistic", "Realistic")
        };
        public static ModOptionString[] webNetOptions = new ModOptionString[2]
        {
            new ModOptionString("Classic", "Classic"),
            new ModOptionString("Realistic", "Realistic")
        };
        public static ModOptionFloat[] alignmentSpeedOption = new ModOptionFloat[10 * 10];

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);
            for(int i = 0; i < intOption.Length; i++)
            {
                intOption[i] = new ModOptionInt(i.ToString(), i);
            }
            for(int i = 0; i < reelOption.Length; i++)
            {
                reelOption[i] = new ModOptionInt((i + 1).ToString(), i+1);
            }
            for(int i = 0; i < webZipPowerOption.Length; i++)
            {
                webZipPowerOption[i] = new ModOptionInt((i + 1).ToString(), i + 1);
            }
            for(int i = 0; i < alignmentSpeedOption.Length; i++)
            {
                alignmentSpeedOption[i] = new ModOptionFloat((i/10f).ToString(), i / 10f);
            }
            for(int i = 0; i < rotationOption.Length; i++)
            {
                rotationOption[i] = new ModOptionInt(i.ToString(), i);
            }
            for(int i = 0; i < waveOption.Length; i++)
            {
                waveOption[i] = new ModOptionFloat((i + 1).ToString(), i + 1);
            }

            for (int i = 0; i < floatOption.Length; i++)
            {
                floatOption[i] = new ModOptionFloat((i / 2f).ToString(), i/2f);
            }
            for(int i = 0; i < strengthOption.Length; i++)
            {
                strengthOption[i] = new ModOptionFloat(i.ToString(), i);
            }
            for(int i = 0; i < jumpPowerOption.Length; i++)
            {
                jumpPowerOption[i] = new ModOptionInt(i.ToString(), i);
            }
        }
        
        [ModOption("Align Player While Swinging", "Enables/Disables aligning the player rotation with the swing arc", category = "Spider-Man")]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        public static bool alignPlayerWhileSwinging;
        [ModOption("Realisitic Webline", "Enables/Disables realistic weblines which shortens the webline when it intersects with another object. Inspired by Spider-man 2 the video game.", category = "Spider-Man")]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        public static bool realisticWeblines;

        [ModOption("Web-Zipping",
            "Enables/Disables the ability to webzip. To Web-Zip, pull your active webline in the opposite direction of the line to pull yourself towards the webline anchor point.",
            category = "Spider-Man")]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        public static bool allowWebZipping;

        [ModOptionArrows]
        [ModOptionCategory("Spider-Man", 13)]
        [ModOption("Web Line Type", "Changes the type of webline used when swinging.", nameof(weblineOptions), defaultValueIndex = 0)]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        public static string webLineType = "Classic";
        
        [ModOptionArrows]
        [ModOptionCategory("Spider-Man", 14)]
        [ModOption("Web Net Type", "Changes the type of webnet used when attachign to enemies.", nameof(weblineOptions), defaultValueIndex = 0)]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        public static string webNetType = "Classic";
        
        /*[ModOption("Auto Reset Align", "Enables/Disables auto adjustment to the normal alignment when Align Player While Swinging is enabled", category = "Spider-Man")]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        public static bool autoResetAlign;*/

        [ModOptionSlider]
        [ModOptionCategory("Spider-Man", 1)]
        [ModOption("Alignment Speed", "Changes the speed at which auto alignment readjusts to the default alignment", nameof(alignmentSpeedOption), defaultValueIndex = 20)]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        public static void AlignmentAdjustSpeed(float value = 2f)
        {
            alignmentSpeed = value;
        }

        [ModOptionSlider]
        [ModOptionCategory("Spider-Man", 2)]
        [ModOption("Web Animation: Quality", "Sets the quality value for the web swing animation.", nameof(intOption), defaultValueIndex = 500)]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        private static void QualitySliderOption(int value = 500)
        {
            quality = value;
        }

        [ModOptionSlider]
        [ModOptionCategory("Spider-Man", 3)]
        [ModOption("Web Animation: Damper", "Sets the damper value for the web swing animation.", nameof(floatOption), defaultValueIndex = 14)]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        private static void DamperSliderOption(float value = 14f)
        {
            damper = value;
        }
        
        [ModOptionSlider]
        [ModOptionCategory("Spider-Man", 4)]
        [ModOption("Web Animation: Strength", "Sets the Strength value for the web swing animation.", nameof(strengthOption), defaultValueIndex = 800)]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        private static void StrengthSliderOption(float value = 800f)
        {
            strength = value;
        }
        
        [ModOptionSlider]
        [ModOptionCategory("Spider-Man", 5)]
        [ModOption("Web Animation: Velocity", "Sets the Velocity value for the web swing animation.", nameof(floatOption), defaultValueIndex = 15)]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        private static void VelocitySliderOption(float value = 15f)
        {
            velocity = value;
        }
        
        [ModOptionSlider]
        [ModOptionCategory("Spider-Man", 6)]
        [ModOption("Web Animation: Wave Count", "Sets the Wave Count value for the web swing animation.", nameof(waveOption), defaultValueIndex = 3)]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        private static void WaveCountSliderOption(float value = 3f)
        {
            waveCount = value;
        }
        
        [ModOptionSlider]
        [ModOptionCategory("Spider-Man", 7)]
        [ModOption("Web Animation: Wave Height", "Sets the Wave Height value for the web swing animation.", nameof(waveOption), defaultValueIndex = 2)]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        private static void WaveHeightSliderOption(float value = 2f)
        {
            waveHeight = value;
        }
        
        [ModOptionSlider]
        [ModOptionCategory("Spider-Man", 8)]
        [ModOption("Web Animation: Rotation", "Sets the Rotation value for the web swing animation.", nameof(rotationOption), defaultValueIndex = 360)]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        private static void RotationSliderOption(int value = 360)
        {
            rotation = value;
        }
        
        [ModOptionSlider]
        [ModOptionCategory("Spider-Man", 9)]
        [ModOption("Web Swiniging: Reel In Power", "Modifies how fast the web pulls you in.", nameof(reelOption), defaultValueIndex = 10)]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        private static void ReelInPowerOption(int value = 10)
        {
            reelInPower = value;
        }
        
        [ModOptionSlider]
        [ModOptionCategory("Spider-Man", 10)]
        [ModOption("Web Swiniging: Reel Out Power", "Modifies how fast the web line lengthens.", nameof(reelOption), defaultValueIndex = 10)]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        private static void ReelOutPowerOption(int value = 10)
        {
            reelOutPower = value;
        }
        
        [ModOptionSlider]
        [ModOptionCategory("Spider-Man", 11)]
        [ModOption("Web-Zip: Power", "Modifies how strong your web-pull is.", nameof(webZipPowerOption), defaultValueIndex = 200)]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        private static void WebZipPowerOption(int value = 200)
        {
            webZipPower = value;
        }
        [ModOptionSlider]
        [ModOptionCategory("Spider-Man", 12)]
        [ModOption("Jump Power", "Modifies how strong your jump is.", nameof(jumpPowerOption), defaultValueIndex = 1)]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        private static void JumpPowerOption(int value = 1)
        {
            jumpPower = value;
        }
    }
}
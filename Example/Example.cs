using System;
using System.Collections.Generic;
using UnityEngine;

namespace Murtagh.Example
{
    public class Example : MonoBehaviour
    {
        public List<NestedExample> NestedExamples;
        
#region Decorator Attributes
        // INFORMATION BOXES : creates an information box in the inspector
        // INFOBOX ATTRIBUTE USAGE EXAMPLE
        [InfoBox("This is an example of an information box.")]
        [InfoBox("Information Box Example", EInfoBoxType.Info)]
        public string InfoBoxExample;
        
        [InfoBox("Warning Box Example", EInfoBoxType.Warning)]
        public string WarningBoxExample;
        
        [InfoBox("Error Box Example", EInfoBoxType.Error)]
        public string ErrorBoxExample;
        
        // HORIZONTAL LINES : creates a horizontal line in the inspector
        // HORIZONTALLINE ATTRIBUTE USAGE EXAMPLE
        [HorizontalLine]
        [HorizontalLine(height: 3f)]
        [HorizontalLine(color: EColor.Pink)]
        [HorizontalLine(height: 5f, color: EColor.Blue)]
        public string HorizontalLineExample;
#endregion
        
#region Grouping Attributes
        // FOLDOUT : creates a foldout with field of the same foldout name grouped inside
        // FOLDOUT ATTRIBUTE USAGE EXAMPLE
        [Foldout("Foldout Example")]
        public int FoldoutInt;
        
        [Foldout("Foldout Example")]
        public float FoldoutFloat;
        
        [Foldout("Foldout Example")]
        public string FoldoutString;
#endregion
        
#region Drawer Attributes
        // READ ONLY : makes a field read-only in the inspector
        // READONLY ATTRIBUTE USAGE EXAMPLE
        [ReadOnly]
        public int ReadOnlyInt;
        [ReadOnly]
        public float ReadOnlyFloat;
        [ReadOnly]
        public string ReadOnlyString;
        
        // RESIZABLE TEXT AREA : makes a string field that automatically resizes based on content
        // RESIZABLETEXTAREA ATTRIBUTE USAGE EXAMPLE
        [ResizableTextArea] 
        public string ResizableTextArea;
#endregion
        
#region Conditional Attributes
        // SHOW IF : shows a field in the inspector only if the condition is met
        // SHOWIF ATTRIBUTE USAGE EXAMPLE
        public bool ShowBool1;

        public bool ShowBool2;

        public IfEnum ShowIfEnum;
        
        public IfEnumFlag ShowIfEnumFlag;

        public IfEnum GetShowEnum()
        {
            return ShowIfEnum;
        }

        public IfEnumFlag GetShowEnumFlag()
        {
            return ShowIfEnumFlag;
        }

        [ShowIf(EConditionOperator.And, "ShowBool1", "ShowBool2")]
        public int ShowIfBoth;

        [ShowIf(EConditionOperator.Or, "ShowBool1", "ShowBool2")]
        public int ShowIfEither;

        [ShowIf("GetShowEnum", IfEnum.Case0)]
        public int ShowIfEnumMatch;

        [ShowIf("GetShowEnumFlag", IfEnumFlag.Flag0)]
        public int ShowIfEnumFlagMatch;
        
        [ShowIf("GetShowEnumFlag", IfEnumFlag.Flag0 | IfEnumFlag.Flag1)]
        public int ShowIfEnumFlagMultipleMatch;
        
        // HIDE IF : hides a field in the inspector if the condition is met
        // HIDEIF ATTRIBUTE USAGE EXAMPLE
        public bool HideBool1;

        public bool HideBool2;

        public IfEnum HideIfEnum;
        
        public IfEnumFlag HideIfEnumFlag;

        public bool GetHide1()
        {
            return HideBool1;
        }

        public bool GetHide2()
        {
            return HideBool2;
        }
        
        public IfEnum GetHideEnum()
        {
            return HideIfEnum;
        }

        public IfEnumFlag GetHideEnumFlag()
        {
            return HideIfEnumFlag;
        }

        [HideIf(EConditionOperator.And, "HideBool1", "HideBool2")]
        public int HideIfBoth;

        [HideIf(EConditionOperator.Or, "HideBool1", "HideBool2")]
        public int HideIfEither;

        [HideIf("GetHideEnum", IfEnum.Case0)]
        public int HideIfEnumMatch;

        [HideIf("GetHideEnumFlag", IfEnumFlag.Flag0)]
        public int HideIfEnumFlagMatch;
        
        [HideIf("GetHideEnumFlag", IfEnumFlag.Flag0 | IfEnumFlag.Flag1)]
        public int HideIfEnumFlagMultipleMatch;

        // ENABLE IF : enables a field in the inspector only if the condition is met
        // ENABLEIF ATTRIBUTE USAGE EXAMPLE
        public bool EnableBool1;

        public bool EnableBool2;

        public IfEnum EnableIfEnum;
        
        public IfEnumFlag EnableIfEnumFlag;

        public bool GetEnable1()
        {
            return EnableBool1;
        }

        public bool GetEnable2()
        {
            return EnableBool2;
        }
        
        public IfEnum GetEnableEnum()
        {
            return EnableIfEnum;
        }

        public IfEnumFlag GetEnableEnumFlag()
        {
            return EnableIfEnumFlag;
        }

        [EnableIf(EConditionOperator.And, "EnableBool1", "EnableBool2")]
        public int EnableIfBoth;

        [EnableIf(EConditionOperator.Or, "EnableBool1", "EnableBool2")]
        public int EnableIfEither;

        [EnableIf("GetEnableEnum", IfEnum.Case0)]
        public int EnableIfEnumMatch;

        [EnableIf("GetEnableEnumFlag", IfEnumFlag.Flag0)]
        public int EnableIfEnumFlagMatch;
        
        [EnableIf("GetEnableEnumFlag", IfEnumFlag.Flag0 | IfEnumFlag.Flag1)]
        public int EnableIfEnumFlagMultipleMatch;
        
        // DISABLE IF : disables a field in the inspector if the condition is met
        // DISABLEIF ATTRIBUTE USAGE EXAMPLE
        public bool DisableBool1;

        public bool DisableBool2;

        public IfEnum DisableIfEnum;
        
        public IfEnumFlag DisableIfEnumFlag;

        public bool GetDisable1()
        {
            return DisableBool1;
        }

        public bool GetDisable2()
        {
            return DisableBool2;
        }
        
        public IfEnum GetDisableEnum()
        {
            return DisableIfEnum;
        }

        public IfEnumFlag GetDisableEnumFlag()
        {
            return DisableIfEnumFlag;
        }

        [DisableIf(EConditionOperator.And, "DisableBool1", "DisableBool2")]
        public int DisableIfBoth;

        [DisableIf(EConditionOperator.Or, "DisableBool1", "DisableBool2")]
        public int DisableIfEither;

        [DisableIf("GetDisableEnum", IfEnum.Case0)]
        public int DisableIfEnumMatch;

        [DisableIf("GetDisableEnumFlag", IfEnumFlag.Flag0)]
        public int DisableIfEnumFlagMatch;
        
        [DisableIf("GetDisableEnumFlag", IfEnumFlag.Flag0 | IfEnumFlag.Flag1)]
        public int DisableIfEnumFlagMultipleMatch;
#endregion


#region Validator Examples
        // MIN MAX VALUE : enforces minimum and/or maximum values for numeric fields in the inspector
        // MINVALUE MAXVALUE ATTRIBUTE USAGE EXAMPLE
        [MinValue(5)]
        public int MinValueIntExample;
        [MaxValue(5)]
        public int MaxValueIntExample;
        
        [MinValue(5.0f)]
        public float MinValueFloatExample;
        [MaxValue(5.0f)]
        public float MaxValueFloatExample;
        
        [MinValue(0), MaxValue(1)]
        public float Range01FloatExample;
        [MinValue(0), MaxValue(1)]
        public Vector2 Range01Vector2Example;
        
        // REQUIRED COMPONENT : shows a warning in the inspector if the field is not assigned
        // REQUIRED ATTRIBUTE USAGE EXAMPLE
        [Required]
        public GameObject RequiredGameObjectExample;
        [Required("This is the custom message for the required field.")]
        public GameObject RequiredGameObjectWithMessageExample;
        
        // VALIDATE INPUT : validates a field's value using a custom validation method
        // VALIDATEINPUT ATTRIBUTE USAGE EXAMPLE
        [ValidateInput("NotZeroValidator")]
        public int ValidateInputExample;
        [ValidateInput("NotZeroValidator", "Value must not be zero.")]
        public int ValidateInputWithMessageExample;

        private bool NotZeroValidator(int value)
        {
            return value != 0;
        }
#endregion
    }
    
    public enum IfEnum
    {
        Case0,
        Case1,
        Case2
    }
    
    [Flags]
    public enum IfEnumFlag
    {
        Flag0 = 1,
        Flag1 = 2,
        Flag2 = 4,
        Flag3 = 8
    }
    
    [Serializable]
    public class NestedExample
    {
        // infobox example
        [InfoBox("Information Box Example", EInfoBoxType.Info)]
        public string InfoBoxExample;
        
        // horizontalline example
        [HorizontalLine(height: 5f, color: EColor.Blue)]
        public string HorizontalLineExample;
        
        // foldout example
        [Foldout("Foldout Example")]
        public int FoldoutInt;
        [Foldout("Foldout Example")]
        public float FoldoutFloat;
        [Foldout("Foldout Example")]
        public string FoldoutString;
        
        // readonly example
        [ReadOnly]
        public int ReadOnlyInt;
        
        // resizabletextarea example
        [ResizableTextArea] 
        public string ResizableTextArea;
        
        // showif example
        public bool ShowBool1;
        public bool ShowBool2;
        [ShowIf(EConditionOperator.And, "ShowBool1", "ShowBool2")]
        public int ShowIfBoth;
        [ShowIf(EConditionOperator.Or, "ShowBool1", "ShowBool2")]
        public int ShowIfEither;
        
        // hideif example
        public bool HideBool1;
        public bool HideBool2;
        [HideIf(EConditionOperator.And, "HideBool1", "HideBool2")]
        public int HideIfBoth;
        [HideIf(EConditionOperator.Or, "HideBool1", "HideBool2")]
        public int HideIfEither;
        
        // enableif example
        public bool EnableBool1;
        public bool EnableBool2;
        [EnableIf(EConditionOperator.And, "EnableBool1", "EnableBool2")]
        public int EnableIfBoth;
        [EnableIf(EConditionOperator.Or, "EnableBool1", "EnableBool2")]
        public int EnableIfEither;
        
        // disableif example
        public bool DisableBool1;
        public bool DisableBool2;
        [DisableIf(EConditionOperator.And, "DisableBool1", "DisableBool2")]
        public int DisableIfBoth;
        [DisableIf(EConditionOperator.Or, "DisableBool1", "DisableBool2")]
        public int DisableIfEither;
        
        // minvalue maxvalue example
        [MinValue(5)]
        public int MinValueIntExample;
        [MaxValue(5)]
        public int MaxValueIntExample;
        
        // required example
        [Required("This is the custom message for the required field.")]
        public GameObject RequiredGameObjectWithMessageExample;
        
        // validateinput example
        [ValidateInput("NotZeroValidator")]
        public int ValidateInputExample;
        
        private bool NotZeroValidator(int value)
        {
            return value != 0;
        }
    }
}
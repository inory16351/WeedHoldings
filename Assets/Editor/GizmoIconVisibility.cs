using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WeedHoldings.EditorTools
{
    /// <summary>
    /// Directional Light / EventSystem은 기능(조명, UI 입력 처리)은 그대로 켜져 있어야 하므로
    /// GameObject를 비활성화하는 대신, Scene/Game 뷰에 뜨는 기즈모 아이콘만 꺼서 화면을 가리지 않게 한다.
    /// Active 체크박스를 끄면 Play 모드에서도 계속 다시 켜야 했던 문제를 코드로 고정 처리.
    /// </summary>
    [InitializeOnLoad]
    static class GizmoIconVisibility
    {
        static GizmoIconVisibility()
        {
            SetIconEnabled(typeof(Light), 108, false);
            SetIconEnabled(typeof(EventSystem), 114, false);
        }

        static void SetIconEnabled(System.Type type, int classID, bool enabled)
        {
            var annotationUtility = typeof(Editor).Assembly.GetType("UnityEditor.AnnotationUtility");
            if (annotationUtility == null) return;

            var setIconEnabled = annotationUtility.GetMethod(
                "SetIconEnabled",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (setIconEnabled == null) return;

            string scriptClass = classID == 114 ? type.Name : string.Empty;
            setIconEnabled.Invoke(null, new object[] { classID, scriptClass, enabled ? 1 : 0 });
        }
    }
}

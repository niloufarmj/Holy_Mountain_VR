using UnityEngine;

public class TouchControllerHandAnimator : MonoBehaviour {
    public Animator animator;
    public OVRInput.Controller controller = OVRInput.Controller.LTouch;
    public string triggerParam = "Trigger";
    public string gripParam    = "Grip";
    void Update() {
        float trigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller);
        float grip    = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger,  controller);
        if (animator) { animator.SetFloat(triggerParam, trigger); animator.SetFloat(gripParam, grip); }
    }
}
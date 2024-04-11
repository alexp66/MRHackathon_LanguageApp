using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.Oculus;


public class HandPinchDetector : MonoBehaviour
{
    [SerializeField] HandPointer handPointer;
    [SerializeField] AudioClip pinchSound;
    [SerializeField] AudioClip releasePinchSound;

    bool hasPinched;
    bool isIndexFingerPinching;
    float pinchStrength;
    OVRHand.TrackingConfidence confidence;


    private void Update()
    {
        CheckPinch(handPointer.rightHand);
    }

    void CheckPinch(OVRHand hand)
    {
        pinchStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        isIndexFingerPinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        confidence = hand.GetFingerConfidence(OVRHand.HandFinger.Index);

        if (handPointer.CurrentTarget) // If it's pointing at a target currently
        {
            Material currentMaterial = handPointer.CurrentTarget.GetComponent<Renderer>().material;
            currentMaterial.SetFloat("_Metallic", pinchStrength); // change material's metallic property based on pinch strength 
        }

        if(!hasPinched && isIndexFingerPinching && confidence == OVRHand.TrackingConfidence.High && handPointer.CurrentTarget)
        {
            hasPinched = true;
            handPointer.CurrentTarget.GetComponent<AudioSource>().PlayOneShot(pinchSound);
        }
        else if(hasPinched && !isIndexFingerPinching)
        {
            hasPinched = false;
            handPointer.CurrentTarget.GetComponent<AudioSource>().PlayOneShot(releasePinchSound);
        }
    }
}

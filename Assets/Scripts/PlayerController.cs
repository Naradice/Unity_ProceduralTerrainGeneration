using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Animation Variables
    public Animator animator;

    // Movement Variables

    float x, z = 0.0f;
    float speed = 0.1f;
    float MinimumX = -90f;
    float MaximumX = 90f;
    public GameObject cam;
    Quaternion cameraRot, characterRot;
    float Xsensityvity = 3f, Ysensityvity = 3f;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        cameraRot = cam.transform.localRotation;
        characterRot = transform.localRotation;
        x = 0;
        z = 0;
    }

    // Update is called once per frame
    void Update()
    {

        UpdateCursorLock();
        
        float xRot = Input.GetAxis("Mouse X") * Xsensityvity;
        float yRot = Input.GetAxis("Mouse Y") * Ysensityvity;
        cameraRot *= Quaternion.Euler(-yRot, 0, 0);
        characterRot *= Quaternion.Euler(0, xRot, 0);
        cameraRot = ClampRotation(cameraRot);
        cam.transform.localRotation = cameraRot;
        transform.localRotation = characterRot;

        if(Mathf.Abs(x) > 0 || Mathf.Abs(z) > 0)
        {
            if(!animator.GetBool("walk")) {
                animator.SetBool("walk", true);
            }
            // if(!animator.GetBool("run")){
            //     //!playerFootstep.isPlaying && 
            //     Debug.Log("Play Foot Step");
            //     PlayerWalkFootStep(walkFootStepSE);
            // }
        }
        else if (animator.GetBool("walk"))
        {
            animator.SetBool("walk", false);
            //StopFootStep();
        }

        if( z > 0 && Input.GetKey(KeyCode.LeftShift))
        {
            if(!animator.GetBool("run")) {
                animator.SetBool("run", true);
                speed = 0.2f;
                //PlayerRunFootStep(runFootStepSE);
            }else{
                //if(!playerFootstep.isPlaying)
                //PlayerRunFootStep(runFootStepSE);
            }
        }
        else if (animator.GetBool("run"))
        {
            animator.SetBool("run", false);
            speed = 0.1f;
            //StopFootStep();
        }
    }

    private void FixedUpdate() 
    {
        x = Input.GetAxisRaw("Horizontal");
        z = Input.GetAxisRaw("Vertical");
        float speed_ratio = speed;

        Vector3 camForward = cam.transform.forward;
        camForward.y = 0;
        if(z < 0)
            speed_ratio = speed * 1/2;

        transform.position += camForward * z * speed_ratio + cam.transform.right * x * speed_ratio;
    }

    private void UpdateCursorLock()
    {
        if(Input.GetKeyDown(KeyCode.Escape)){
            Cursor.lockState = CursorLockMode.None;
        }
        else if(Input.GetMouseButtonUp(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public Quaternion ClampRotation(Quaternion q)
    {
        q.x /= q.w;
        q.y /= q.w;
        q.z /= q.w;
        q.w = 1f;

        float angleX = Mathf.Atan(q.x) * Mathf.Rad2Deg * 2f;
        
        angleX = Mathf.Clamp(angleX, MinimumX, MaximumX);

        q.x = Mathf.Tan(angleX * Mathf.Deg2Rad * 0.5f);

        return q;
    }
}

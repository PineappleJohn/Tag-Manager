using Photon.Pun;
using UnityEngine;
using static JohnTagManager;

public class TagTrigger : MonoBehaviour
{
    public JohnTagManager tagManager;
    public CurrentGamemode gamemode;
    public PhotonView view;
    readonly float maxDist = 2.3f;

    bool ct = true;

    public void GetGamemode()
    {
        gamemode = tagManager.currentGamemode;
    }

    public void ToggleTagAbility()
    {
        ct = !ct;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Tag") && ct)
        {
            if (Vector3.Distance(transform.position, tagManager.gameObject.transform.position) > maxDist) return; //little anticheat for any tag guns out there
            GetGamemode();

            bool a = false;

            switch (gamemode)
            {
                case CurrentGamemode.Casual:
                    return;
                case CurrentGamemode.Tag: a = true; break;
                case CurrentGamemode.Infection: a = false; break;
            }

            if (other && tagManager)
                tagManager.TagSomeone(other.GetComponent<JohnTagManager>().me, a);
            else
            {
                Debug.Log("This or another client may have left, or a bug occured. (TagTrigger at 34)");
            }
        }
    }
}
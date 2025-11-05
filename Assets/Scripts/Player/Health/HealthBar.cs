using UnityEngine;
using UnityEngine.UI;

public class Healthbar : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private Image totalhealthBar;
    [SerializeField] private Image currenthealthBar;

    // @SFX:UIHealthInit
    private void Start()
    {
        totalhealthBar.fillAmount = playerHealth.currentHealth / 100;
    }
    // @SFX:UIHealthUpdate
    private void Update()
    {
        currenthealthBar.fillAmount = playerHealth.currentHealth / 100;
    }
}
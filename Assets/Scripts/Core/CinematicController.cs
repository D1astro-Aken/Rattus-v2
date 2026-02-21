using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CinematicController : MonoBehaviour
{
    [System.Serializable]
    public class CinematicStep
    {
        public string stepName; // Pro přehlednost v editoru
        public Transform targetPosition; // Bod, kam má kamera dojet
        public float moveSpeed = 5f; // Rychlost pohybu kamery
        public bool useEasing = true; // Použít Ease-In Ease-Out?
        public float waitTime = 1f; // Jak dlouho čekat po dojetí (a po spuštění animace)
        
        [Header("Animation Settings")]
        public Animator targetAnimator; // Postava nebo objekt, který se má animovat
        public string triggerName; // Název Triggeru v Animatoru (např. "Attack", "Wave")
    }

    [Header("General Settings")]
    public Camera mainCamera; // Odkaz na hlavní kameru
    public KeyCode triggerKey = KeyCode.J; // Klávesa pro spuštění
    
    [Header("Cinematic Sequence")]
    public List<CinematicStep> steps; // Seznam kroků minifilmu

    [Header("Events")]
    public UnityEvent onCinematicStart;
    public UnityEvent onCinematicFinished;

    private bool isPlaying = false; // Abychom nespouštěli vícekrát najednou

    private void Start()
    {
        // Pokud chcete spustit minifilm hned po startu scény, odkomentujte následující řádek:
        // StartCinematic();
    }

    private void Update()
    {
        if (Input.GetKeyDown(triggerKey) && !isPlaying)
        {
            StartCinematic();
        }
    }

    // Tuto metodu zavolejte pro spuštění (např. přes Trigger nebo tlačítko)
    public void StartCinematic()
    {
        if (isPlaying) return;
        StartCoroutine(PlayCinematicRoutine());
    }

    private IEnumerator PlayCinematicRoutine()
    {
        isPlaying = true;
        onCinematicStart.Invoke();

        // Projdeme všechny kroky v seznamu
        foreach(var step in steps)
        {
            if (step.targetPosition == null) continue;

            // --- FÁZE POHYBU ---
            Vector3 startPos = mainCamera.transform.position;
            // Získáme cílovou pozici (zachováme Z souřadnici kamery, obvykle -10)
            Vector3 targetPos = new Vector3(step.targetPosition.position.x, step.targetPosition.position.y, mainCamera.transform.position.z);
            
            float distance = Vector3.Distance(startPos, targetPos);
            
            // Pokud je vzdálenost velmi malá, přeskočíme pohyb
            if (distance > 0.01f)
            {
                if (step.useEasing)
                {
                    // === Ease-In Ease-Out pohyb ===
                    float duration = distance / step.moveSpeed; // Čas = Vzdálenost / Rychlost
                    float elapsed = 0f;

                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(elapsed / duration);
                        
                        // SmoothStep vytvoří ease-in ease-out křivku
                        float smoothT = Mathf.SmoothStep(0f, 1f, t);
                        
                        mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
                        yield return null;
                    }
                }
                else
                {
                    // === Lineární pohyb (původní) ===
                    while (Vector3.Distance(mainCamera.transform.position, targetPos) > 0.01f)
                    {
                        mainCamera.transform.position = Vector3.MoveTowards(
                            mainCamera.transform.position, 
                            targetPos, 
                            step.moveSpeed * Time.deltaTime
                        );
                        yield return null;
                    }
                }
                
                // Ujištění se, že jsme přesně na konci
                mainCamera.transform.position = targetPos;
            }

            // --- FÁZE ANIMACE ---
            if (step.targetAnimator != null && !string.IsNullOrEmpty(step.triggerName))
            {
                step.targetAnimator.SetTrigger(step.triggerName);
            }

            // --- FÁZE ČEKÁNÍ ---
            if (step.waitTime > 0)
            {
                yield return new WaitForSeconds(step.waitTime);
            }
        }

        onCinematicFinished.Invoke();
        isPlaying = false;
    }
}

using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private Player_Movement player;
    [SerializeField] private CrabDialogue dialogue;

    [Header("Teksty Kraba (mozna edytowac w Inspectorze)")]
    [SerializeField, TextArea]
    private string[] introLines =
    {
        "Ahoy, matey! Welcome aboard.",
        "I'm the Crab and I'll show you how to survive out here."
    };

    [SerializeField, TextArea]
    private string[] runLines =
    {
        "First things first: walking."
    };
    [SerializeField, TextArea] private string runHint = "Use A and D to run left and right.";

    [SerializeField, TextArea]
    private string[] jumpLines =
    {
        "Not bad! But there are gaps ahead."
    };
    [SerializeField, TextArea] private string jumpHint = "Press SPACE to jump.";

    [SerializeField, TextArea]
    private string[] doubleJumpLines =
    {
        "Nice jump! Some gaps are too wide for one jump, though."
    };
    [SerializeField, TextArea] private string doubleJumpHint = "Press SPACE again in the air to jump twice.";

    [SerializeField, TextArea]
    private string[] swordLines =
    {
        "Wait... I see you have a sword!",
        "Do you know how to use it? Oh, you're all alone... let me show you."
    };
    [SerializeField, TextArea] private string attackHint = "Press Q or the left mouse button to attack.";

    [SerializeField, TextArea]
    private string[] finalLines =
    {
        "Perfect! Now go ahead - there's an enemy waiting for you. Good luck!"
    };

    private bool jumped;
    private bool doubleJumped;
    private bool attacked;

    private void OnDestroy()
    {
        if (player == null) return;
        player.Jumped -= OnJumped;
        player.DoubleJumped -= OnDoubleJumped;
        player.Attacked -= OnAttacked;
    }

    private void OnJumped() => jumped = true;
    private void OnDoubleJumped() => doubleJumped = true;
    private void OnAttacked() => attacked = true;

    private IEnumerator Start()
    {
        player.Jumped += OnJumped;
        player.DoubleJumped += OnDoubleJumped;
        player.Attacked += OnAttacked;

        // Na starcie: tylko rozmowa, brak sterowania, wszystkie mechaniki zablokowane
        player.SetJumpUnlocked(false);
        player.SetDoubleJumpUnlocked(false);
        player.SetAttackUnlocked(false);
        player.SetInputLocked(true);

        // Powitanie
        yield return dialogue.ShowLines(introLines);

        // Bieg lewo/prawo
        yield return dialogue.ShowLines(runLines);
        dialogue.Show(runHint);
        player.SetInputLocked(false);

        bool sawLeft = false;
        bool sawRight = false;
        while (!(sawLeft && sawRight))
        {
            if (player.MoveInput < 0f) sawLeft = true;
            if (player.MoveInput > 0f) sawRight = true;
            yield return null;
        }

        dialogue.Hide();
        player.SetInputLocked(true);

        // Skok
        yield return dialogue.ShowLines(jumpLines);
        player.SetJumpUnlocked(true);
        dialogue.Show(jumpHint);
        jumped = false;
        player.SetInputLocked(false);

        yield return new WaitUntil(() => jumped);
        yield return WaitForLanding();

        dialogue.Hide();
        player.SetInputLocked(true);

        // Podwojny skok
        yield return dialogue.ShowLines(doubleJumpLines);
        player.SetDoubleJumpUnlocked(true);
        dialogue.Show(doubleJumpHint);
        doubleJumped = false;
        player.SetInputLocked(false);

        yield return new WaitUntil(() => doubleJumped);
        yield return WaitForLanding();

        dialogue.Hide();
        player.SetInputLocked(true);

        // Miecz i atak
        yield return dialogue.ShowLines(swordLines);
        player.SetAttackUnlocked(true);
        dialogue.Show(attackHint);
        attacked = false;
        player.SetInputLocked(false);

        yield return new WaitUntil(() => attacked);
        yield return new WaitForSeconds(0.6f);

        // Zakonczenie: Krab zachecza do walki z pierwszym przeciwnikiem
        player.SetInputLocked(true);
        yield return dialogue.ShowLines(finalLines);
        player.SetInputLocked(false);
    }

    // Czeka, az gracz wyladuje (krotka pauza, zeby zdazyl oderwac sie od ziemi)
    private IEnumerator WaitForLanding()
    {
        yield return new WaitForSeconds(0.2f);
        yield return new WaitUntil(() => player.IsGrounded);
    }
}
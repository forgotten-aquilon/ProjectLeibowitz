using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerScript : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxLives = 3;
    [SerializeField, Min(1)] private int contactDamage = 1;
    [SerializeField, Min(0f)] private float contactInvulnerability = 1f;

    private PlayerMovement playerMovement;
    private float nextContactDamageTime;

    public int Lives { get; private set; }
    public int MaxLives => maxLives;
    public bool IsDead => Lives == 0;

    public event Action<int> LivesChanged;
    public event Action Died;

    private void Awake()
    {
        Lives = maxLives;
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void OnCollisionEnter(Collision collision) => HandleMonsterContact(collision);

    private void OnCollisionStay(Collision collision) => HandleMonsterContact(collision);

    private void HandleMonsterContact(Collision collision)
    {
        if (IsDead || Time.time < nextContactDamageTime)
            return;

        var monster = collision.collider.GetComponentInParent<MonsterChaser>();
        if (!monster)
            return;

        nextContactDamageTime = Time.time + contactInvulnerability;
        playerMovement.KnockBack(transform.position - monster.transform.position);
        TakeDamage(contactDamage);
    }

    public void TakeDamage(int amount = 1)
    {
        if (amount <= 0 || IsDead)
            return;

        Lives = Mathf.Max(0, Lives - amount);
        LivesChanged?.Invoke(Lives);
        if (IsDead)
            Died?.Invoke();
    }

    public void RestoreLives()
    {
        if (Lives == maxLives)
            return;

        Lives = maxLives;
        LivesChanged?.Invoke(Lives);
    }
}

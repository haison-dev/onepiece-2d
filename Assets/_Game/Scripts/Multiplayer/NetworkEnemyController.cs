using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(EnemyHealth))]
public sealed class NetworkEnemyController : NetworkBehaviour
{
    private readonly NetworkVariable<int> synchronizedHealth = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private EnemyHealth enemyHealth;
    private KizaruAoeAttack aoeAttack;
    private bool despawnPending;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        aoeAttack = GetComponent<KizaruAoeAttack>();
    }

    public override void OnNetworkSpawn()
    {
        enemyHealth.ConfigureNetworkedDeath();
        synchronizedHealth.OnValueChanged += HandleNetworkHealthChanged;

        if (aoeAttack != null)
            aoeAttack.ConfigureNetworkRole(IsServer);

        if (IsServer)
        {
            enemyHealth.HealthChanged += HandleServerHealthChanged;
            synchronizedHealth.Value = enemyHealth.CurrentHealth;

            if (aoeAttack != null)
            {
                aoeAttack.AttackStarted += HandleServerAttackStarted;
                aoeAttack.DamageApplied += HandleServerDamageApplied;
            }
        }
        else if (synchronizedHealth.Value > 0)
        {
            enemyHealth.ApplyNetworkHealth(synchronizedHealth.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        synchronizedHealth.OnValueChanged -= HandleNetworkHealthChanged;

        if (enemyHealth != null)
            enemyHealth.HealthChanged -= HandleServerHealthChanged;

        if (aoeAttack != null)
        {
            aoeAttack.AttackStarted -= HandleServerAttackStarted;
            aoeAttack.DamageApplied -= HandleServerDamageApplied;
        }
    }

    private void LateUpdate()
    {
        if (!despawnPending || !IsServer || !IsSpawned)
            return;

        despawnPending = false;
        HideDefeatedEnemyRpc();
        NetworkObject.Despawn(false);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void HideDefeatedEnemyRpc()
    {
        if (aoeAttack != null)
            aoeAttack.enabled = false;

        foreach (Renderer enemyRenderer in GetComponentsInChildren<Renderer>(true))
            enemyRenderer.enabled = false;

        foreach (Collider2D enemyCollider in GetComponentsInChildren<Collider2D>(true))
            enemyCollider.enabled = false;
    }

    private void HandleServerHealthChanged(int currentHealth, int maxHealth)
    {
        if (!IsServer)
            return;

        synchronizedHealth.Value = currentHealth;
        if (currentHealth <= 0)
            despawnPending = true;
    }

    private void HandleNetworkHealthChanged(int previousValue, int newValue)
    {
        if (!IsServer)
            enemyHealth.ApplyNetworkHealth(newValue);
    }

    private void HandleServerAttackStarted(Vector2 impactPosition)
    {
        PlayRemoteAttackRpc(impactPosition);
    }

    [Rpc(SendTo.NotServer)]
    private void PlayRemoteAttackRpc(Vector2 impactPosition)
    {
        if (aoeAttack != null)
            aoeAttack.PlayRemoteAttack(impactPosition);
    }

    private void HandleServerDamageApplied(Vector3 worldPosition, int amount)
    {
        ShowPlayerDamageRpc(worldPosition, amount);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ShowPlayerDamageRpc(Vector3 worldPosition, int amount)
    {
        DamagePopup.Show(worldPosition, amount, DamagePopupType.PlayerDamage);
    }
}

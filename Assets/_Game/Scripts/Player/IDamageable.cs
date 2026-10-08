public interface IDamageable
{
    int CurrentHealth { get; }
    bool IsDead { get; }
    void TakeDamage(int damage);
}

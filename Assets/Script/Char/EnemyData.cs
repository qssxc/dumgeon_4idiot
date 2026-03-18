using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy", menuName = "Roguelike/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("기본 정보")]
    public string enemyName;   // 이름 (예: Rat)
    public Sprite sprite;      // 몬스터 그림

    [Header("전투 스탯")]
    public int maxHp;          // 최대 체력
    public int damage;         // 공격력
    public int viewRange;      // 시야 범위
}
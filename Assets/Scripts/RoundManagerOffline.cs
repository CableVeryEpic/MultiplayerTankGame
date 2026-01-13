using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Cinemachine;
using UnityEditor.Rendering;
using UnityEngine;

public class RoundManagerOffline : MonoBehaviour
{
    [Header("Round Settings")]
    [SerializeField] private int totalRounds = 5;
    [SerializeField] private float intermissionSeconds = 5f;

    [Header("Spawning")]
    [SerializeField] private Participant tankPrefab;
    [SerializeField] private int playerCount = 2;

    public CinemachineCamera cam;

    private readonly List<Participant> participants = new();
    private SpawnPoint[] spawnPoints;

    private int currentRoundIndex = 0;
    private bool roundActive = false;
    private float nextRoundStartTime = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);

        if (spawnPoints.Length < playerCount)
        {
            Debug.LogError($"Not enough SpawnPoints. Have {spawnPoints.Length}, need {playerCount}.");
            return;
        }
    }

    private void Start()
    {
        SpawnTanksOnce();
        StartNextRound();
    }

    // Update is called once per frame
    void Update()
    {
        if (roundActive)
        {
            int aliveCount = participants.Count(t => t != null && t.IsAlive);
            if (aliveCount <= 1)
            {
                EndRound();
            }
        }
        else
        {
            if (Time.time >= nextRoundStartTime && currentRoundIndex < totalRounds)
            {
                StartNextRound();
            }
        }
    }

    private void SpawnTanksOnce()
    {
        participants.Clear();

        for (int i = 0; i < playerCount; i++)
        {
            var tank = Instantiate(tankPrefab);
            tank.name = $"Tank_{i + 1}";
            if (i == 0)
                cam.Follow = tank.transform;
            participants.Add(tank);
        }
    }

    private void StartNextRound()
    {
        if (currentRoundIndex >= totalRounds) return;

        currentRoundIndex++;
        roundActive = true;

        Debug.Log($"--- ROUND {currentRoundIndex}/{totalRounds} START ---");

        var shuffled = spawnPoints.OrderBy(x => Random.value).ToArray();

        for (int i = 0; i < participants.Count; i++)
        {
            if (participants[i] != null)

                participants[i].ResetForRound(shuffled[i % shuffled.Length]);
            if (i > 0)
                participants[i].SetControlsEnabled(false); // Turn off controls for everyone except player 1, for testing
        }
    }

    private void EndRound()
    {
        roundActive = false;

        var winner = participants.FirstOrDefault(t => t != null && t.IsAlive);
        if (winner != null)
        {
            Debug.Log($"--- ROUND {currentRoundIndex} END: WINNER IS {winner.name} ---");
        }
        else
        {
            Debug.Log($"--- ROUND {currentRoundIndex} END: NO WINNER, ALL DEAD ---");
        }

        if (currentRoundIndex >= totalRounds)
        {
            Debug.Log("--- ALL ROUNDS COMPLETE ---");
            return;
        }

        nextRoundStartTime = Time.time + intermissionSeconds;
    }
}

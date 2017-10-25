﻿using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

public class RoundManager : MonoBehaviour {
    /*  状态机划分依据：
     *  1.执行一次和每帧执行之间切换。
     *  2.可以明确的阶段。
     */
    
    public event EventHandler GameStarted;
    public event EventHandler GameEnded;
    public event EventHandler RoundStarted;
    public event EventHandler RoundEnded;
    public event EventHandler TurnStarted;
    public event EventHandler TurnEnded;
    public event EventHandler UnitEnded;

    private static RoundManager instance;

    public static RoundManager GetInstance()
    {
        return instance;
    }
    public int NumberOfPlayers { get; private set; }

    public RoundState _roundState;

    public RoundState RoundState
    {
        private get
        {
            return _roundState;
        }
        set
        {
            if (_roundState != null)
                _roundState.OnStateExit();
            _roundState = value;
            _roundState.OnStateEnter();
        }
    }

    public Unit CurrentUnit { get; set; }

    public int CurrentPlayerNumber { get; private set; }

    public Transform playersParent;

    public List<Player> Players { get; private set; }

    private List<Unit> Units { get; set; }

    public int roundNumber = 0;

    public float gameStartTime = 2f;                 //状态持续时间。
    public float roundStartTime = 2f;                //状态持续时间。
    public float turnStartTime = 2f;                 //状态持续时间。

    IEnumerator GameStart()
    {
        if (GameStarted != null)
            GameStarted.Invoke(this, new EventArgs());
        //角色加入忽略层
        Units.ForEach(u => u.gameObject.layer = 2);
        yield return new WaitForSeconds(gameStartTime);
        StartCoroutine(RoundStart());
    }
    
    IEnumerator RoundStart()
    {
        roundNumber++;
        if (RoundStarted != null)
            RoundStarted.Invoke(this, new EventArgs());
        //角色加入忽略层
        Units.ForEach(u => u.gameObject.layer = 2);
        yield return new WaitForSeconds(roundStartTime);
        
        Units.ForEach(u => { u.OnRoundStart(); });
        StartCoroutine(TurnStart());
    }

    IEnumerator TurnStart()
    {
        Units.ForEach(u => { u.OnTurnStart(); });
        if (TurnStarted != null)
            TurnStarted.Invoke(this, new EventArgs());
        yield return new WaitForSeconds(turnStartTime);
        
        Players.Find(p => p.playerNumber.Equals(CurrentPlayerNumber)).Play(this);
        //角色取出忽略层
        Units.ForEach(u => u.gameObject.layer = 0);
    }

    void Awake()
    {
        instance = this;
    }

    public void EndTurn()
    {
        if (Units.Select(u => u.playerNumber).Distinct().Count() == 1)
            return;

        if (UnitEnded != null)
            UnitEnded.Invoke(this, new EventArgs());
        if (Units.FindAll(u => u.playerNumber == CurrentPlayerNumber && u.UnitEnded == false).Count > 0)    //当前玩家仍有角色未操作。
        {
            Players.Find(p => p.playerNumber.Equals(CurrentPlayerNumber)).Play(this);
            
            
        }
        if (Units.FindAll(u => u.playerNumber == CurrentPlayerNumber && u.UnitEnded == false).Count == 0)   //当前Player的所有Unit执行完毕
        {
            
            CurrentPlayerNumber = (CurrentPlayerNumber + 1) % NumberOfPlayers;
            while (Units.FindAll(u => u.playerNumber.Equals(CurrentPlayerNumber)).Count == 0)
            {
                CurrentPlayerNumber = (CurrentPlayerNumber + 1) % NumberOfPlayers;
            }//Skipping players that are defeated.
            
            if (Units.FindAll(u => u.UnitEnded == false).Count == 0)    //所有Player的所有Unit执行完毕
            {
                EndRound();
            }
            else
            {
                Units.ForEach(u => { u.OnTurnEnd(); });
                if (TurnEnded != null)
                    TurnEnded.Invoke(this, new EventArgs());
                StartCoroutine(TurnStart());
            }
        }
    }

    public void EndRound()
    {
        Units.ForEach(u => { u.OnRoundEnd(); });
        if (RoundEnded != null)
            RoundEnded.Invoke(this, new EventArgs());
        StartCoroutine(RoundStart());
        
        //CellGridState = new CellGridStateTurnChanging(this);
    }

    void Start () {
        Players = new List<Player>();
        Units = UnitManager.GetInstance().units;
        Units.ForEach(u => { u.Initialize(); });
        for (int i = 0; i < playersParent.childCount; i++)
        {
            var player = playersParent.GetChild(i).GetComponent<Player>();
            if (player != null)
            {
                Players.Add(player);
            }
            else
                Debug.LogError("Invalid object in Players Parent game object");
        }

        foreach (var unit in Units)
        {
            unit.UnitClicked += OnUnitClicked;
            unit.UnitDestroyed += OnUnitDestroyed;
            //设置同盟列表。
            
        }

        NumberOfPlayers = Players.Count;
        CurrentPlayerNumber = Players.Min(p => p.playerNumber);
        StartCoroutine(GameStart());
    }

    private void OnUnitClicked(object sender, EventArgs e)
    {
        RoundState.OnUnitClicked(sender as Unit);
    }

    private void OnUnitDestroyed(object sender, EventArgs e)
    {
        var totalPlayersAlive = Units.Select(u => u.playerNumber).Distinct().ToList(); //Checking if the game is over
        if (totalPlayersAlive.Count == 1)
        {
            if (GameEnded != null)
                GameEnded.Invoke(this, new EventArgs());
            if(totalPlayersAlive[0] == 0)
            {
                Debug.Log("Win!");
            }
            else
            {
                Debug.Log("Lose");
            }
        }
    }

    public void Restart()
    {
        SceneManager.LoadScene("SLG");
    }

    public void AddUnit(Unit unit)
    {
        unit.UnitClicked += OnUnitClicked;
        unit.UnitDestroyed += OnUnitDestroyed;
    }
}
// ====================================================================================================================
//
// Created by Leslie Young
// http://www.plyoung.com/ or http://plyoung.wordpress.com/
// ====================================================================================================================

using UnityEngine;
using System.Collections.Generic;

public class GameController : TMNController 
{
	// ====================================================================================================================
	#region inspector properties

	public CameraMove camMover;					// used to move camera around (like make it follow a transform)
	public SelectionIndicator selectionMarker;	// used to indicate which unit is active/selected
	public RadiusMarker attackRangeMarker;		// show how far the selected unit can attack at

	public GameObject[] unitFabs;				// unit prefabs
	
	// these are samples of ways you might like to handle the visible markers
	// please optimise to your needs by removing this and the if() statements
	public bool hideSelectorOnMove = true;		// hide the selection marker when a unit moves?
	public bool hideMarkersOnMove = true;		// hide the node markers when a unit moves?

	public bool useTurns = true;				// show example of using limited moves?

	public bool combatOn = true;				// combat is only shown in sample 1, so turn of for other

	#endregion
	// ====================================================================================================================
	#region vars

	private enum State : byte { Init=0, Running, DontRun }
	private State state = State.Init;

	private Unit selectedUnit = null;	// currently selected unit
	private TileNode hoverNode = null;	// that that mouse is hovering over
	private TileNode prevNode = null;	// helper during movement

	public bool allowInput { get; set; }

	public List<Unit>[] units = 
	{
		new List<Unit>()	// Units
	};
	

	public int currPlayerTurn  { get; set; }		// which player's turn it is, only if useTurns = true;
	public int currentPlayerIndex = 0;
	
	public GameObject[] playerUnits;
	public TileNode[] playerNodes;
	
	public GameObject[] AIUnits;
	public TileNode[] AINodes;
	


	#endregion
	// ====================================================================================================================
	#region start/init

	public override void Start()
	{
		base.Start();
		allowInput = false;
		currPlayerTurn = 0;
		state = State.Init;
	}
	
	private void generateUnits() 
	{
		int count = units.Length;
		
		for (int i = 0; i < count; i++)
		{
			int playerCount = playerUnits.Length;
			
			for (int p = 0; p < playerCount; p++)
			{
				Unit playerUnit = playerUnits[p].GetComponent<Unit>();
				TileNode playerNode = playerNodes[p].GetComponent<TileNode>();
				
				Unit player = (Unit)Unit.SpawnUnit(playerUnit.gameObject, map, playerNode);				
				player.Init(OnUnitEvent);
				units[i].Add(player);
			}
			int AIcount = AIUnits.Length;
			
			for (int a = 0; a < AIcount; a++)
			{
				Unit AIUnit = AIUnits[a].GetComponent<Unit>();
				TileNode AINode = AINodes[a].GetComponent<TileNode>();
				
				Unit AI = (Unit)Unit.SpawnUnit(AIUnit.gameObject, map, AINode);				
				AI.Init(OnUnitEvent);
				units[i].Add(AI);
			}
		}		
	}



	#endregion
	// ====================================================================================================================
	#region update/input

	public void Update()
	{
		if (state == State.Running)
		{
			// check if player clicked on tiles/units. You could choose not to call this in certain frames,
			// for example if your GUI handled the input this frame and you don't want the player 
			// clicking 'through' GUI elements onto the tiles or units

			if (allowInput) this.HandleInput();

		}

		else if (state == State.Init)
		{
			state = State.Running;
			generateUnits();
			//SpawnRandomUnits(spawnCount);
			allowInput = true;
		}
	}

	#endregion
	// ====================================================================================================================
	#region pub

	public void ChangeTurn()
	{
		currPlayerTurn = (currPlayerTurn == 0 ? 1 : 0);

		// unselect any selected unit
		OnClearNaviUnitSelection(null);

		// reset active player's units
		foreach (Unit u in units[currPlayerTurn])
		{
			u.Reset();
		}
	}

	#endregion
	// ====================================================================================================================
	#region input handlers - click tile

	protected override void OnTileNodeClick(GameObject go)
	{
		base.OnTileNodeClick(go);
		TileNode node = go.GetComponent<TileNode>();
		if (selectedUnit != null && node.IsVisible)
		{
			prevNode = selectedUnit.node; // needed if unit is gonna move
			if (selectedUnit.MoveTo(node, ref selectedUnit.currMoves))
			{
				// dont want the player clicking around while a unit is moving
				allowInput = false;

				// hide the node markers when unit is moving. Note that the unit is allready linked with
				// the destination node by now. So use the cached node ref
				if (hideMarkersOnMove) prevNode.ShowNeighbours(((Unit)selectedUnit).maxMoves, false);

				// hide the selector
				if (hideSelectorOnMove) selectionMarker.Hide();

				// hide the attack range indicator
				if (combatOn) attackRangeMarker.HideAll();

				// camera should follow the unit that is moving
				camMover.Follow(selectedUnit.transform);
			}
		}
	}

	protected override void OnTileNodeHover(GameObject go)
	{
		base.OnTileNodeHover(go);
		if (go == null)
		{	// go==null means TMNController wanna tell that mouse moved off but not onto another visible tile
			if (hoverNode != null)
			{
				hoverNode.OnHover(false);
				hoverNode = null;
			}
			return;
		}

		TileNode node = go.GetComponent<TileNode>();
		if (selectedUnit != null && node.IsVisible)
		{
			if (hoverNode != node)
			{
				if (hoverNode != null) hoverNode.OnHover(false);
				hoverNode = node;
				node.OnHover(true);
			}
		}
		else if (hoverNode != null)
		{
			hoverNode.OnHover(false);
			hoverNode = null;
		}
	}

	#endregion
	// ====================================================================================================================
	#region input handlers - click unit

	protected override void OnNaviUnitClick(GameObject go)
	{
		base.OnNaviUnitClick(go);

		Unit unit = go.GetComponent<Unit>();

		// jump camera to the unit that was clicked on
		camMover.Follow(go.transform);

		// -----------------------------------------------------------------------
		// using turns sample?
		if (useTurns)
		{
			// is active player's unit that was clicked on?
			if (unit.playerSide == (currPlayerTurn + 1))
			{
				selectedUnit = go.GetComponent<Unit>();

				// move selector to the clicked unit to indicate it's selection
				selectionMarker.Show(go.transform);

				// show the nodes that this unit can move to
				selectedUnit.node.ShowNeighbours(selectedUnit.currMoves, selectedUnit.tileLevel, true, true);

				// show how far this unit can attack at, if unit did not attack yet this turn
				if ( !selectedUnit.didAttack && combatOn)
				{
					attackRangeMarker.Show(selectedUnit.transform.position, selectedUnit.attackRange);
				}
			}

			// else, not active player's unit but his opponent's unit that was clicked on
			else if (selectedUnit!=null && combatOn)
			{
				if (selectedUnit.Attack(unit))
				{
					allowInput = false;
					attackRangeMarker.HideAll();
				}
			}
		}

		// -----------------------------------------------------------------------
		// not using turns sample
		else
		{
			bool changeUnit = true;

			// first check if opposing unit was clicked on that can be attacked
			if (selectedUnit != null && combatOn)
			{
				if (selectedUnit.Attack(unit))
				{
					changeUnit = false;
					allowInput = false;

					// if not using turns sample, then reset didAttack now so it can attack again if it wanted to
					selectedUnit.didAttack = false;

					attackRangeMarker.HideAll();
				}
			}

			if (changeUnit)
			{
				selectedUnit = unit;

				// move selector to the clicked unit to indicate it's selection
				selectionMarker.Show(go.transform);

				// show the nodes that this unit can move to
				selectedUnit.node.ShowNeighbours(selectedUnit.currMoves, selectedUnit.tileLevel, true, true);

				// show how far this unit can attack at, if unit did not attack yet this turn
				if (combatOn) attackRangeMarker.ShowOutline(selectedUnit.transform.position, selectedUnit.attackRange);

				// show how far this unit can attack at, if unit did not attack yet this turn
				if (!selectedUnit.didAttack && combatOn)
				{
					attackRangeMarker.Show(selectedUnit.transform.position, selectedUnit.attackRange);
				}
			}
		}
	}

	protected override void OnClearNaviUnitSelection(GameObject clickedAnotherUnit)
	{
		base.OnClearNaviUnitSelection(clickedAnotherUnit);
		bool canClear = true;

		// if clicked on another unit i first need to check if can clear
		if (clickedAnotherUnit != null && selectedUnit != null)
		{
			Unit unit = clickedAnotherUnit.GetComponent<Unit>();
			if (useTurns)
			{
				// Don't clear if opponent unit was cleared and using Turns example.
				if (unit.playerSide != selectedUnit.playerSide) canClear = false;
			}

			else
			{
				// in this case, only clear if can't attack the newly clicked unit
				if (selectedUnit.CanAttack(unit)) canClear = false;
			}
		}

		// -----------------------------------------------------------------------
		if (canClear)
		{
			// hide the selection marker
			selectionMarker.Hide();

			// hide targeting marker
			if (combatOn) attackRangeMarker.HideAll();

			if (selectedUnit != null)
			{
				// hide the nodes that where shown when unit was clicked, this way I only touch the nodes that I kow I activated
				// note that map.DisableAllTileNodes() could also be used by would go through all nodes
				selectedUnit.node.ShowNeighbours(((Unit)selectedUnit).maxMoves, false);
				selectedUnit = null;
			}
			else
			{
				// just to be sure, since OnClearNaviUnitSelection() was called while there was no selected unit afterall
				map.ShowAllTileNodes(false);
			}
		}
	}

	#endregion
	// ====================================================================================================================
	#region callbacks

	/// <summary>called when a unit completed something, like moving to a target node</summary>
	private void OnUnitEvent(NaviUnit unit, int eventCode)
	{
		// eventcode 1 = unit finished moving
		if (eventCode == 1)
		{
			if (!hideMarkersOnMove && prevNode != null)
			{	// the markers where not hidden when the unit started moving,
				// then they should be now as they are invalid now
				prevNode.ShowNeighbours(((Unit)selectedUnit).maxMoves, false);
			}

			// do a fake click on the unit to "select" it again
			this.OnNaviUnitClick(unit.gameObject);
			allowInput = true; // allow input again
			
		}

		// eventcode 2 = unit done attacking
		if (eventCode == 2)
		{
			allowInput = true; // allow input again

			if (!useTurns)
			{
				this.OnNaviUnitClick(unit.gameObject);
			}
		}
	}

	#endregion
	// ====================================================================================================================
}

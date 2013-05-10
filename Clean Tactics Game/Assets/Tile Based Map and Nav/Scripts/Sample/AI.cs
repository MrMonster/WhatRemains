using UnityEngine;
using System.Collections.Generic;

// The AI player in a vsAI game
public class AI : Unit
{

	private enum State : byte { pause, running, waiting, building, ending_phase }
	private State state = State.running;

	private float timer = 0f;
	private bool isStartOfTurn = true;
	private Unit currUnit = null;
	private Unit lastUnloadCarrier = null;
	private bool landPathToOppStillOpen = true;

	// ****************************************************************************************************************
	#region init

	public AI(bool is_player_one, bool is_blue, string id, string name, GameController TMNController)
		: base(is_player_one, is_blue, id, name, TMNController)
	{
		state = State.running;
		isStartOfTurn = true;
		currUnit = null;
	}

	#endregion
	// ****************************************************************************************************************
	#region update and input

	public override void Update()
	{
		if (state == State.running)
		{
				if (isStartOfTurn) UpdateStartOfTurn();
				UpdateCombat();

		}

		else if (state == State.ending_phase)
		{
			timer -= Time.deltaTime;
			if (timer <= 0.0f)
			{
				state = State.running;
				game.NextPhase();
			}
		}
	}

	#endregion
	// ****************************************************************************************************************
	#region Start of turn

	private void UpdateStartOfTurn()
	{
		isStartOfTurn = false;
	}
	#endregion
	// ****************************************************************************************************************
	#region Combat Phase

	private void UpdateCombat()
	{
		if (currUnit == null)
		{
			currUnit = FindNextAvailUnit(false);
		}

		if (currUnit == null)
		{	
			// no more units to act on
			state = State.ending_phase;
			timer = 0.5f;
		}
		else
		{
			if (currUnit.kind == Game.UnitKind.Combat)
			{
				ActCombatUnit();
			}
			else
			{
				currUnit.currMoves = 0;
				currUnit.didAttack = true;
				currUnit = null;
			}
		}
	}

	private void ActCombatUnit()
	{
		if (AttemptAnAttack(currUnit))
		{	// attacking, so return now
			return;
		}

		if (!currUnit.HappyWithPosition && currUnit.currMoves > 0)
		{
			
				foreach (Unit target in game.notActivePlayer.units)
				{
					if (MoveToPostionCloseToTarget(currUnit, target))
					{	// moving, so return now
						return;
					}
				}
		}
		else
		{
			currUnit.currMoves = 0; // dont wanna move further
		}

		if (AttemptAnAttack(currUnit))
		{	// attacking, so return now
			return;
		}

		// done with unit
		currUnit.currMoves = 0;
		currUnit.didAttack = true;
		currUnit = null;
	}

	private bool AttemptAnAttack(Unit unit)
	{
		if (unit.didAttack) return false;

		// *** 1. Is there something close that I could kill?
		List<Unit> closeUnits = FindUnitsItCanAttackFromNode(unit.node, unit.range);
		if (closeUnits.Count > 0)
		{
			foreach (Unit u in closeUnits)
			{
				if (u.currHp <= unit.damage)
				{	// i could kill this one, attack it
					if (unit.Attack(u, OnUnitEvent))
					{
						if (game.selectedUnit == u) game.ClearUnitSelection();
						game.cm.Follow(unit.transform);
						state = State.waiting;
						return true; // return now and wait for attack to finish
					}
				}
			}
		}

		List<Unit> onTheWayUnits = new List<Unit>();
		List<NavNode> onTheWayNodes = new List<NavNode>();
		if (unit.currMoves > 0)
		{
			// *** 2. Is there something I could kill on my way to destination?
			//		If so, be sure to first stop and kill it before moving on. So set a new Destination and move there to kill opp unit.
			if (unit.DestinationNode != null)
			{
				NavNode[] points = game.mapData.navMesh.CalcPath(unit.node, unit.DestinationNode, (unit.type==Game.UnitType.Flyer), true);
				if (points != null) if (points.Length > 0)
				{
					int allowedMoves = points.Length;
					if (allowedMoves > unit.currMoves) allowedMoves = unit.currMoves;
					for (int i = 0; i < allowedMoves; i++)
					{
						List<Unit> add_units = FindUnitsItCanAttackFromNode(points[i], unit.range);
						foreach(Unit u in add_units)
						{
							if (!onTheWayUnits.Contains(u))
							{
								onTheWayUnits.Add(u);
								onTheWayNodes.Add(points[i]);
							}
						}
					}
				}				
			}

			// got a list? check which of them i could kill and move there now
			if (onTheWayUnits.Count > 0)
			{
				foreach (Unit u in onTheWayUnits)
				{
					if (u.currHp <= unit.damage)
					{	// i could kill this one, move closer
						int idx = onTheWayUnits.IndexOf(u);
						if (idx >= 0 && idx<onTheWayNodes.Count)
						{
							if (unit.MoveAI(game.mapData.navMesh, onTheWayNodes[idx], OnUnitEvent))
							{
								game.cm.Follow(unit.transform);
								state = State.waiting;
								return true; // wait while it is moving there
							}
						}
					}
				}
			}
		}

		// *** 3. So there would be nothing I could kill? Is there anything I can attack before moving away?
		if (closeUnits.Count > 0)
		{
			foreach (Unit u in closeUnits)
			{
				if (unit.Attack(u, OnUnitEvent))
				{
					if (game.selectedUnit == u && u.currHp - unit.damage <= 0) game.ClearUnitSelection();

					game.cm.Follow(unit.transform);
					state = State.waiting;
					return true; // return now and wait for attack to finish
				}
			}
		}

		// onTheWayUnits will only be set if got to do the calcs in ***2 above
		if (onTheWayUnits.Count > 0)
		{
			// *** 4. If there was nothing, would there be anything I could attack on my way? 
			// If so, be sure to stop and do the attack before moving further.
			if (onTheWayUnits.Count > 0)
			{
				foreach (Unit u in onTheWayUnits)
				{
					int idx = onTheWayUnits.IndexOf(u);
					if (idx >= 0 && idx<onTheWayNodes.Count)
					{
						if (unit.MoveAI(game.mapData.navMesh, onTheWayNodes[idx], OnUnitEvent))
						{
							game.cm.Follow(unit.transform);
							state = State.waiting;
							return true; // wait while it is moving there
						}
					}
				}
			}
		}

		return false;
	}

	public List<Unit> FindUnitsItCanAttackFromNode(TileNode fromNode, int range)
	{
		List<Unit> res = new List<Unit>();
		foreach (Unit u in game.notActivePlayer.units)
		{
			if (CanAttackFromNode(fromNode, u, range)) res.Add(u);
		}
		return res;
	}

	public bool CanAttackFromNode(TileNode fromNode, Unit target, int range)
	{
		// if the two units are on the same tile then they cannot attack each other
		// air unit cant attack right underneath it and land cant attack above it
		if (target == fromNode.airUnit || target == fromNode.landUnit) return false;

		// now check if it is in range of the target
		// first check if not next to target, in which case below tests will be overkill
		if (fromNode.IsNeighbour(target.node))
		{
			return true;
		}

		if (fromNode.IsInRange(target.node, range))
		{	// is in range, now check if there is a big obstacle like a wall in the way
			fromNode.TurnOWallColliders(range, true);

			Vector3 sourcePos = fromNode.transform.position + new Vector3(0f, 0.5f, 0f);
			Vector3 targetPos = target.node.transform.position + new Vector3(0f, 0.5f, 0f);

			Vector3 direction = targetPos - sourcePos;
			float distance = direction.magnitude + 0.5f;
			RaycastHit hit;
			bool canAttack = true;
			int mask = ~(1 << 20);  // everything except unit layer
			if (Physics.Raycast(sourcePos, direction, out hit, distance, mask))
			{
				canAttack = false;
			}

			// done, turn them colliders off
			fromNode.TurnOWallColliders(range, false);
			return canAttack;
		}
		return false;
	}

	private bool MoveToPostionCloseToTarget(Unit unit, Unit target)
	{
		// find a tile i can move to and from which I could shoot at target
		// find any tile that I could stand on and be in range, starting with furthest away
		List<NavNode> targetNodes = target.node.GetListOfNeighbours(unit.range);
		if (targetNodes.Count==0) return false;
		targetNodes.Reverse(); // want the further nodes first in list

		bool reachViaAir = false;
		foreach (NavNode node in targetNodes)
		{
			reachViaAir = false; // reset

			// do some basic checks before doing the can attack check
			if (unit.type == Game.UnitType.Flyer)
			{
				// flying unit can go to just about anywhere (walsl and buildinsg are not incl in list, so no need to test for them)
				if (node.airUnit != null) continue; // air unit in way
			}
			else
			{
				// land units will have tougher time
				if (node.tileType == NavNode.TileType.air) continue; // eish
				if (node.landUnit != null) continue; // unit in way there

				// check if can reach on foot
				if ( CanReachDestination(unit, node, false) == 0 )
				{	// maybe via air?
					reachViaAir = true;
				}
			}

			// now check if could attack from tile and move there if can
			if (CanAttackFromNode(node, target, unit.range))
			{	// found a tile i could shoot from

				if (reachViaAir && unit.type != Game.UnitType.Flyer)
				{	// unit can move there, but only through air, so wait for carrier
					unit.DestinationNode = node;
					unit.WaitingForCarrier = true;
					if (unit.kind == Game.UnitKind.Engineer) carrierQueue.Insert(0, unit);
					else carrierQueue.Add(unit);
					unit.currMoves = 0;
					return true; // return true cause it is waiting for carrier
				}

				else
				{
					if (unit.MoveAI(game.mapData.navMesh, node, OnUnitEvent))
					{	// moving to it now
						game.cm.Follow(unit.transform);
						state = State.waiting;
						unit.DestinationNode = node;
						return true;
					}
				}
			}
		}
		
		return false;
	}


	private int CanReachDestination(Unit unit, TileNode dest, bool viaAir)
	{
		NavNode[] points = game.mapData.navMesh.CalcPath(unit.node, dest, viaAir, true);
		if (points == null) return 0; // cant move to that node
		if (points.Length == 0) return 0; // cant move to that node
		return points.Length; // seems fine
	}



	#endregion
	// ****************************************************************************************************************
	#region workers

	private Unit FindNextAvailUnit()
	{		
		foreach (Unit u in this.units)
		{
				if (u.kind == Game.UnitKind.Combat)
				{
					// normal combat unit
					if (u.didAttack == false || u.currMoves > 0)
					{
						return u;
					}
					
				}
				
		}
		return null;
	}

	private Unit FindUnitOfKind(GameController.UnitKind kind, bool onlyIfNotAttacked)
	{
		foreach (Unit u in this.units)
		{
			if (u.kind == kind)
			{
				if (onlyIfNotAttacked)
				{
					if (u.didAttack == false) return u;
				}
				else return u;
			}
		}
		return null;
	}

	#endregion
	// ****************************************************************************************************************
	#region callbacks

	public void OnUnitEvent(Unit theUnit, Unit.UnitEvent e)
	{
		
		state = State.running;

		if (currUnit == null) return;

		if (currUnit.kind == Game.UnitKind.Combat)
		{	// if these are true then the unit is done for the turn. 
			// these WILL be set by rest of AI code even if unit dont attack or move
			if (currUnit.currMoves== 0 && currUnit.didAttack) currUnit = null;
		}
		else if (currUnit.kind == Game.UnitKind.Engineer)
		{
			if (currUnit.currMoves == 0) currUnit = null;
		}
	}

	#endregion
	// ****************************************************************************************************************
}

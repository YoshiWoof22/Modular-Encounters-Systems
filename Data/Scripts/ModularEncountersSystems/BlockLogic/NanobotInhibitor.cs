﻿using ModularEncountersSystems.Entities;
using ModularEncountersSystems.Tasks;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace ModularEncountersSystems.BlockLogic {
	public class NanobotInhibitor : BaseBlockLogic, IBlockLogic {

		internal IMyRadioAntenna _antenna;

		internal double _disableRange;
		internal double _antennaRange;

		internal List<MyDefinitionId> _targetedBlocks;

		internal List<BlockEntity> _potentialBlocks;
		internal List<BlockEntity> _blocksInRange;

		internal List<PlayerEntity> _playersInRange;

		public NanobotInhibitor(BlockEntity block) {

			Setup(block);

		}

		internal override void Setup(BlockEntity block) {

			_tamperCheck = true;
			base.Setup(block);

			if (!_isServer) {

				_isValid = false;
				return;

			}

			_blocksInRange = new List<BlockEntity>();
			_potentialBlocks = new List<BlockEntity>();

			_playersInRange = new List<PlayerEntity>();

			BlockManager.GetBlocksOfTypes(_potentialBlocks, BlockManager.NanobotBlockIds);
			BlockManager.BlockAdded += GetNewBlock;

			_antenna = block.Block as IMyRadioAntenna;
			_antenna.Radius = 10000;
			_logicType = "Nanobot Inhibitor";
			_useTick100 = true;
			_antenna.CustomName = "[Nanobot Inhibitor Field]";
			_antenna.CustomNameChanged += NameChange;

		}

		internal void GetNewBlock(BlockEntity block) {

			if (!block.ActiveEntity())
				return;

			if (!BlockManager.NanobotBlockIds.Contains(block.Block.SlimBlock.BlockDefinition.Id))
				return;

			_potentialBlocks.Add(block);
		
		}

		public override void WorkingChanged(IMyCubeBlock block = null) {

			base.WorkingChanged(block);

			if (!_isWorking)
				return;

			RunTick100();

			for (int i = 0; i < _blocksInRange.Count; i++) {

				var target = _blocksInRange[i];

				if (!target.ActiveEntity())
					continue;

				target.FunctionalBlock.Enabled = false;

			}

		}

		internal void EnableChanged(IMyTerminalBlock block) {

			if (!_isWorking || !Active)
				return;

			var funcBlock = block as IMyFunctionalBlock;

			if (funcBlock == null)
				return;

			funcBlock.Enabled = false;
		
		}

		internal void NameChange(IMyTerminalBlock block) {
		
			if(_antenna.CustomName != "[Nanobot Inhibitor Field]")
				_antenna.CustomName = "[Nanobot Inhibitor Field]";

		}

		internal override void RunTick100() {

			if (!_isWorking || !Active)
				return;

			if (_antenna.Radius != _antennaRange) {

				_antennaRange = _antenna.Radius;
				_disableRange = _antenna.Radius;

			}

			//Player Messages
			for (int i = PlayerManager.Players.Count - 1; i >= 0; i--) {

				var player = PlayerManager.Players[i];

				if (!player.ActiveEntity()) {

					continue;
				
				}

				if (player.Distance(_antenna.GetPosition()) < _disableRange) {

					if (!_playersInRange.Contains(player)) {

						MyVisualScriptLogicProvider.ShowNotification("WARNING: Inhibitor Field Has Disable Nanobot Functionality!", 4000, "Red", player.Player.IdentityId);
						_playersInRange.Add(player);

					}

				} else {

					if (_playersInRange.Contains(player)) {

						_playersInRange.Remove(player);

					}

				}

			}

			//Get New Blocks
			for (int i = _potentialBlocks.Count - 1; i >= 0; i--) {

				var block = _potentialBlocks[i];

				if (!block.ActiveEntity()) {

					_potentialBlocks.RemoveAt(i);
					_blocksInRange.Remove(block);
					continue;

				}

				if (block.Distance(_antenna.GetPosition()) < _disableRange) {

					if (!_blocksInRange.Contains(block)) {

						_blocksInRange.Add(block);
						block.FunctionalBlock.EnabledChanged += EnableChanged;
						EnableChanged(block.Block);

					}

				} else {

					if (_blocksInRange.Contains(block)) {

						RemoveBlock(block);

					}
				
				}
			
			}
						
		}

		
		internal void RemoveBlock(BlockEntity block) {

			if (block.ActiveEntity())
				block.FunctionalBlock.EnabledChanged -= EnableChanged;

			_blocksInRange.Remove(block);
		
		}

		internal override void Unload(IMyEntity entity = null) {

			base.Unload(entity);

			BlockManager.BlockAdded -= GetNewBlock;

			if (_antenna != null) {

				_antenna.CustomNameChanged -= NameChange;

			}

			foreach (var block in _blocksInRange) {

				if (block.ActiveEntity())
					block.FunctionalBlock.EnabledChanged -= EnableChanged;


			}
		
		}

	}

}

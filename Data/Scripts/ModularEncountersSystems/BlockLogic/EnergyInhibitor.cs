﻿using ModularEncountersSystems.Entities;
using ModularEncountersSystems.Tasks;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ModularEncountersSystems.BlockLogic {
	public class EnergyInhibitor : BaseBlockLogic, IBlockLogic {

		internal IMyRadioAntenna _antenna;
		internal IMyTerminalBlock _block;

		internal double _antennaRange;

		internal bool _playersInRange;
		internal List<PlayerEntity> _playersInBlockRange;

		internal float _damageAtZeroDistance = 0.25f;

		public EnergyInhibitor(BlockEntity block) {

			Setup(block);

		}

		internal override void Setup(BlockEntity block) {

			_tamperCheck = true;
			base.Setup(block);

			if (!_isServer) {

				_isValid = false;
				return;

			}

			_playersInBlockRange = new List<PlayerEntity>();
			_antenna = block.Block as IMyRadioAntenna;
			_block = block.Block as IMyTerminalBlock;

			if (_antenna != null) {

				_antenna.Radius = 800;
				_antenna.CustomName = "[Energy Inhibitor Field]";
				_antenna.CustomNameChanged += NameChange;

			} else {

				_antennaRange = 800;

			}

			_logicType = "Energy Inhibitor";
			_useTick60 = true;
			_useTick100 = true;

		}

		internal void NameChange(IMyTerminalBlock block) {
		
			if(_antenna.CustomName != "[Energy Inhibitor Field]")
				_antenna.CustomName = "[Energy Inhibitor Field]";

		}

		internal override void RunTick60() {

			if (!_isWorking || !Active || !_playersInRange)
				return;

			foreach (var player in _playersInBlockRange) {

				if (player?.Player?.Character == null || !player.ActiveEntity() || player.IsParentEntitySeat)
					continue;

				float distanceRatio = 1 - (float)(Vector3D.Distance(player.GetPosition(), Entity.GetPosition()) / _antennaRange);
				float existingEnergy = MyVisualScriptLogicProvider.GetPlayersEnergyLevel(player.Player.IdentityId);
				float newEnergy = MathHelper.Clamp(existingEnergy - (_damageAtZeroDistance * distanceRatio), 0, 1);
				MyVisualScriptLogicProvider.SetPlayersEnergyLevel(player.Player.IdentityId, newEnergy);

			}

		}

		internal override void RunTick100() {

			if (!_isWorking || !Active)
				return;

			if (_antenna != null && _antenna.Radius != _antennaRange) {

				_antennaRange = _antenna.Radius;

			}

			//Check Player Distances and Status
			foreach (var player in PlayerManager.Players) {

				if (!player.ActiveEntity() || player.IsParentEntitySeat || (player.PlayerInhibitorNullifier != null && player.PlayerInhibitorNullifier.EffectActive())) {

					RemovePlayer(player);
					continue;

				}

				var distance = player.Distance(Entity.GetPosition());

				if (distance > _antennaRange) {

					RemovePlayer(player);
					continue;

				}

				if (distance <= _antennaRange) {

					if (!_playersInBlockRange.Contains(player)) {

						MyVisualScriptLogicProvider.ShowNotification("WARNING: Energy Inhibitor Detected. Suit Energy Rapidly Depleting.", 5000, "Red", player.Player.IdentityId);
						_playersInBlockRange.Add(player);

					}

				}
			
			}

			_playersInRange = _playersInBlockRange.Count > 0;

		}

		internal void RemovePlayer(PlayerEntity player) {

			_playersInBlockRange.Remove(player);
		
		}

		internal override void Unload(IMyEntity entity = null) {

			base.Unload(entity);

			if (_antenna != null) {

				_antenna.CustomNameChanged -= NameChange;

			}
		
		}

	}

}

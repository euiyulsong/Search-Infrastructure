<?php
	// constructs a player
	class Player {
		private $name;
		private $team;
		private $gp;
		private $min;
		private $fg;
		private $pt;
		private $ft;
		private $rebound;
		private $ppg;
		private $ast;

		// build a player with given list of information of the player
		function __construct($player) {
			$this->name = $player['Name'];
			$this->team = $player['Team'];
			$this->gp = $player['GP'];
			$this->min = $player['Min'];
			$this->fg = $player['Pct-FG'];
			$this->pt = $player['Pct-3PT'];
			$this->ft = $player['Pct-FT'];
			$this->rebound = $player['Tot-Rb'];
			$this->ppg = $player['PPG'];
			$this->ast = $player['Ast'];
		}

		// returns the name of the player
		public function getName() {
			return "<p class=\"name\">".$this->name."</p>";
		}

		// returns the team of the player
		public function getTeam() {
			return "<p>".$this->team."</p>";
		}

		// returns the GP of the player
		public function getGP() {
			return "<p>GP: ".$this->gp."</p>";
		}

		// returns the Min of the player
		public function getMin() {
			return "<p>Min: ".$this->min."</p>";
		}

		// returns the FG Pct of the player
		public function getFG() {
			return "<p>FG Pct: ".$this->fg."</p>";
		}

		// returns the 3PT Pct of the player
		public function get3PT() {
			return "<p>3PT Pct: ".$this->pt."</p>";
		}

		// returns the FT Pct of the player
		public function getFT() {
			return "<p>FT Pct: ".$this->ft."</p>";
		}

		// returns the rebounds of the player
		public function getRebound() {
			return "<p>Rebound: ".$this->rebound."</p>";
		}

		// returns the PPG of the player
		public function getPPG() {
			return "<p>PPG: ".$this->ppg."</p>";
		}

		// returns the Ast of the player
		public function getAst() {
			return "<p>Ast: ".$this->ast."</p>";
		}

		// returns the information of the player
		public static function player($row) {
			$result = new Player($row);
			return "<div id=\"player\">".$result->getName().$result->getTeam().$result->getGP().$result->getMin().$result->getFG().$result->get3PT().$result->getFT().$result->getRebound().$result->getPPG().$result->getAst()."</div>";
		}
	}
?>
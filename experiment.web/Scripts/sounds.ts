/// <reference path="typings/webaudioapi/waa.d.ts" />
module Experiment {
    export class LamdbaSounds {
        public startGeneric: HTMLAudioElement;
        public endingGeneric: HTMLAudioElement;
        public choc: HTMLAudioElement;

        constructor() {
            this.startGeneric = new Audio("/content/choc.mp3");//new Audio("/content/starting.mp3");
            this.endingGeneric = new Audio("/content/choc.mp3");//new Audio("/content/ending.mp3");
            this.choc = new Audio("/content/choc.mp3");
        }
    }

    export class AlphaSounds extends LamdbaSounds {

        public start_alpha: HTMLAudioElement;
        constructor() {
            super();
            this.start_alpha = new Audio("/content/choc.mp3");//new Audio("/content/start-alpha.mp3");
        }
    }
} 
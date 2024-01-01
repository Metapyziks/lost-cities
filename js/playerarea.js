import Expedition from "./expedition.js";
import Hand from "./hand.js";
import { rotate } from "./helpers.js";
export default class PlayerArea {
    constructor(offsetX, offsetY, angle) {
        let handX = 0;
        let handY = -2;
        [handX, handY] = rotate(handX, handY, angle);
        this.hand = new Hand(offsetX + handX, offsetY + handY, angle);
        this.expeditions = new Map();
        for (let i = 1; i <= 5; ++i) {
            let x = (i - 3) * 6;
            let y = -24;
            [x, y] = rotate(x, y, angle);
            this.expeditions.set(i, new Expedition(offsetX + x, offsetY + y, angle));
        }
    }
    clear() {
        this.hand.clear();
        for (let expedition of this.expeditions.values()) {
            expedition.clear();
        }
    }
}

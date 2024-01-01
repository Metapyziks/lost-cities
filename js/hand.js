import CardCollection from "./cardcollection.js";
export default class Hand extends CardCollection {
    constructor(offsetX, offsetY, angle) {
        super({
            offsetX: offsetX,
            offsetY: offsetY,
            offsetZ: 20,
            angle: angle,
            sort: true,
            radius: 96,
            angleIncrement: 3
        });
    }
}

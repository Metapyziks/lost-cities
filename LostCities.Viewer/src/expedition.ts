import CardCollection from "./cardcollection.js";

export default class Expedition extends CardCollection {
    constructor(offsetX: number, offsetY: number, angle: number) {
        super({
            offsetX: offsetX,
            offsetY: offsetY,
            angle: angle,
            sort: false,

            yIncrement: 1.5
        });
    }
}

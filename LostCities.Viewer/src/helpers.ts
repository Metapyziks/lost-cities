export function rotate(x: number, y: number, angle: number): [x: number, y: number] {
    const rads = angle * Math.PI / 180;
    const cos = Math.cos(rads);
    const sin = Math.sin(rads);
    return [cos * x + sin * y, cos * y - sin * x];
}

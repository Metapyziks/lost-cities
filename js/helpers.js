export function rotate(x, y, angle) {
    const rads = angle * Math.PI / 180;
    const cos = Math.cos(rads);
    const sin = Math.sin(rads);
    return [cos * x + sin * y, cos * y - sin * x];
}

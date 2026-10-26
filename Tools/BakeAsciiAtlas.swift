// Bakes the glyph atlas used by InkCamera: one row of fixed cells, white glyphs on transparent.
// Usage: swift Tools/BakeAsciiAtlas.swift <font.ttf> <cellWidth> <cellHeight> <fontPx> <out.bytes>
// Text glyphs come from the font; box-drawing and block glyphs are drawn so they tile seamlessly.
import CoreGraphics
import CoreText
import Foundation
import ImageIO
import UniformTypeIdentifiers

let a = CommandLine.arguments
guard a.count == 6, let cw = Int(a[2]), let ch = Int(a[3]), let px = Double(a[4]) else {
    fatalError("usage: <font.ttf> <cellW> <cellH> <fontPx> <out>")
}
let glyphs = try! String(contentsOfFile: "Assets/Resources/AsciiGlyphs.txt", encoding: .utf8)
    .replacingOccurrences(of: "\n", with: "")
let chars = Array(glyphs)
let descs = CTFontManagerCreateFontDescriptorsFromURL(URL(fileURLWithPath: a[1]) as CFURL) as! [CTFontDescriptor]
let font = CTFontCreateWithFontDescriptor(descs[0], px, nil)
let ascent = CTFontGetAscent(font), descent = CTFontGetDescent(font)
let baseline = ((Double(ch) - ascent - descent) / 2 + descent).rounded()
let w = chars.count * cw
let ctx = CGContext(data: nil, width: w, height: ch, bitsPerComponent: 8, bytesPerRow: w * 4,
                    space: CGColorSpaceCreateDeviceRGB(), bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue)!
ctx.setAllowsFontSmoothing(false)
ctx.setShouldSubpixelPositionFonts(false)
ctx.setFillColor(CGColor(red: 1, green: 1, blue: 1, alpha: 1))

func rect(_ x: Int, _ y: Int, _ rw: Int, _ rh: Int, _ ox: Int) { ctx.fill(CGRect(x: ox + x, y: y, width: rw, height: rh)) }

let t = max(1, ch / 14)              // stroke thickness
let mx = (cw - t) / 2, my = (ch - t) / 2
for (i, c) in chars.enumerated() {
    let ox = i * cw
    // Lines: l = left, r = right, u = up, d = down; double lines use two strokes.
    func lines(_ spec: String, double: Bool = false) {
        let offsets = double ? [-t, t] : [0]
        for o in offsets {
            if spec.contains("l") { rect(0, my + o, mx + t, t, ox) }
            if spec.contains("r") { rect(mx, my + o, cw - mx, t, ox) }
            if spec.contains("u") { rect(mx + o, my, t, ch - my, ox) }
            if spec.contains("d") { rect(mx + o, 0, t, my + t, ox) }
        }
    }
    func shade(_ keep: (Int, Int) -> Bool) { for y in 0..<ch { for x in 0..<cw where keep(x, y) { rect(x, y, 1, 1, ox) } } }
    switch c {
    case "─": lines("lr"); case "│": lines("ud")
    case "┌": lines("rd"); case "┐": lines("ld"); case "└": lines("ru"); case "┘": lines("lu")
    case "├": lines("udr"); case "┤": lines("udl"); case "┬": lines("lrd"); case "┴": lines("lru"); case "┼": lines("lrud")
    case "═": lines("lr", double: true); case "║": lines("ud", double: true)
    case "╔": rect(mx - t, my + t, cw - mx + t, t, ox); rect(mx - t, 0, t, my + 2 * t, ox)
              rect(mx + t, my - t, cw - mx - t, t, ox); rect(mx + t, 0, t, my, ox)
    case "╗": rect(0, my + t, mx + 2 * t, t, ox); rect(mx + t, 0, t, my + 2 * t, ox)
              rect(0, my - t, mx, t, ox); rect(mx - t, 0, t, my, ox)
    case "╚": rect(mx - t, my - t, cw - mx + t, t, ox); rect(mx - t, my - t, t, ch - my + t, ox)
              rect(mx + t, my + t, cw - mx - t, t, ox); rect(mx + t, my + t, t, ch - my - t, ox)
    case "╝": rect(0, my - t, mx + 2 * t, t, ox); rect(mx + t, my - t, t, ch - my + t, ox)
              rect(0, my + t, mx, t, ox); rect(mx - t, my + t, t, ch - my - t, ox)
    case "█": rect(0, 0, cw, ch, ox)
    case "▀": rect(0, ch / 2, cw, ch - ch / 2, ox); case "▄": rect(0, 0, cw, ch / 2, ox)
    case "▌": rect(0, 0, cw / 2, ch, ox); case "▐": rect(cw / 2, 0, cw - cw / 2, ch, ox)
    case "░": shade { x, y in x % 2 == 0 && y % 4 == (x % 4 == 0 ? 0 : 2) }
    case "▒": shade { x, y in (x + y) % 2 == 0 }
    case "▓": shade { x, y in !(x % 2 == 0 && y % 4 == (x % 4 == 0 ? 0 : 2)) }
    default:
        let line = CTLineCreateWithAttributedString(NSAttributedString(string: String(c),
            attributes: [NSAttributedString.Key(kCTFontAttributeName as String): font,
                         NSAttributedString.Key(kCTForegroundColorFromContextAttributeName as String): true]))
        let adv = CTLineGetTypographicBounds(line, nil, nil, nil)
        ctx.saveGState()
        ctx.clip(to: CGRect(x: ox, y: 0, width: cw, height: ch))
        ctx.textPosition = CGPoint(x: Double(ox) + ((Double(cw) - adv) / 2).rounded(), y: baseline)
        CTLineDraw(line, ctx)
        ctx.restoreGState()
    }
}
let dest = CGImageDestinationCreateWithURL(URL(fileURLWithPath: a[5]) as CFURL, UTType.png.identifier as CFString, 1, nil)!
CGImageDestinationAddImage(dest, ctx.makeImage()!, nil)
CGImageDestinationFinalize(dest)
print("\(chars.count) glyphs, \(cw)x\(ch), ascent \(ascent) descent \(descent) baseline \(baseline)")

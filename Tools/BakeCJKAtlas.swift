// Bakes the wide-character atlas for Chinese / Japanese interface text: a grid of `cols` cells of cw x ch pixels.
// Each character takes two neighbouring cells (left half at an even index 2i, right half at 2i + 1), so the text grid
// can print it across two columns. White glyphs on transparent, like BakeAsciiAtlas.swift.
// Usage: swift Tools/BakeCJKAtlas.swift <font.ttf> <cellWidth> <cellHeight> <fontPx> <chars.txt> <out.bytes> [cols]
import CoreGraphics
import CoreText
import Foundation
import ImageIO
import UniformTypeIdentifiers

let a = CommandLine.arguments
guard a.count >= 7, let cw = Int(a[2]), let ch = Int(a[3]), let px = Double(a[4]) else {
    fatalError("usage: <font.ttf> <cellW> <cellH> <fontPx> <chars.txt> <out> [cols]")
}
let cols = a.count > 7 ? Int(a[7])! : 64
let chars = Array(try! String(contentsOfFile: a[5], encoding: .utf8).replacingOccurrences(of: "\n", with: ""))
let rows = max(1, (chars.count * 2 + cols - 1) / cols)
let descs = CTFontManagerCreateFontDescriptorsFromURL(URL(fileURLWithPath: a[1]) as CFURL) as! [CTFontDescriptor]
let font = CTFontCreateWithFontDescriptor(descs[0], px, nil)
let ascent = CTFontGetAscent(font), descent = CTFontGetDescent(font)
let w = cols * cw, h = rows * ch
let ctx = CGContext(data: nil, width: w, height: h, bitsPerComponent: 8, bytesPerRow: w * 4,
                    space: CGColorSpaceCreateDeviceRGB(), bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue)!
ctx.setAllowsFontSmoothing(false)
ctx.setFillColor(CGColor(red: 1, green: 1, blue: 1, alpha: 1))
let baseline = ((Double(ch) - ascent - descent) / 2 + descent).rounded()
for (i, c) in chars.enumerated() {
    let k = i * 2, col = k % cols, row = k / cols
    let ox = Double(col * cw), oy = Double(h - (row + 1) * ch)        // CoreGraphics origin is bottom-left
    let line = CTLineCreateWithAttributedString(NSAttributedString(string: String(c),
        attributes: [NSAttributedString.Key(kCTFontAttributeName as String): font,
                     NSAttributedString.Key(kCTForegroundColorFromContextAttributeName as String): true]))
    let adv = CTLineGetTypographicBounds(line, nil, nil, nil)
    ctx.saveGState()
    ctx.clip(to: CGRect(x: ox, y: oy, width: Double(cw * 2), height: Double(ch)))
    ctx.textPosition = CGPoint(x: ox + ((Double(cw * 2) - adv) / 2).rounded(), y: oy + baseline)
    CTLineDraw(line, ctx)
    ctx.restoreGState()
}
let dest = CGImageDestinationCreateWithURL(URL(fileURLWithPath: a[6]) as CFURL, UTType.png.identifier as CFString, 1, nil)!
CGImageDestinationAddImage(dest, ctx.makeImage()!, nil)
CGImageDestinationFinalize(dest)
print("\(chars.count) wide glyphs, \(cols)x\(rows) cells of \(cw)x\(ch)")

;; invoke: div (f32:3.3) (f32:2.2)
;; expect: (f32:1.5)

(module
    (func (export "div") (param $x f32) (param $y f32) (result f32)
        local.get $x
        local.get $y
        f32.div
    )
)
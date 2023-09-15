;; invoke: sub (f32:3.45) (f32:2.34)
;; expect: (f32:1.11)

(module
    (func (export "sub") (param $x f32) (param $y f32) (result f32)
        local.get $x
        local.get $y
        f32.sub
    )
)
;; invoke: mul (f32:2) (f32:3)
;; expect: (f32:6)

(module
    (func (export "mul") (param $x f32) (param $y f32) (result f32)
        local.get $x
        local.get $y
        f32.mul
    )
)
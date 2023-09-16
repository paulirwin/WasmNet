;; invoke: mul (f64:2) (f64:3)
;; expect: (f64:6)

(module
    (func (export "mul") (param $x f64) (param $y f64) (result f64)
        local.get $x
        local.get $y
        f64.mul
    )
)
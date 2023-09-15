;; invoke: sub (f64:3.45) (f64:2.34)
;; expect: (f64:1.11)

(module
    (func (export "sub") (param $x f64) (param $y f64) (result f64)
        local.get $x
        local.get $y
        f64.sub
    )
)
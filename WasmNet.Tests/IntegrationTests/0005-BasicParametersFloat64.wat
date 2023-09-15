;; invoke: add (f64:1.23) (f64:2.34)
;; expect: (f64:3.57)

(module
    (func (export "add") (param $x f64) (param $y f64) (result f64)
        local.get $x
        local.get $y
        f64.add
    )
)
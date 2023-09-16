;; invoke: div (f64:3.3) (f64:2.2)
;; expect: (f64:1.5)

(module
    (func (export "div") (param $x f64) (param $y f64) (result f64)
        local.get $x
        local.get $y
        f64.div
    )
)
;; invoke: gt
;; expect: (i32:0)

(module
    (func (export "gt") (result i32)
        f64.const -42.0
        f64.const 2.1
        f64.gt
    )
)
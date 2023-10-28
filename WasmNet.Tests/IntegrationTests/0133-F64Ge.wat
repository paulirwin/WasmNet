;; invoke: ge
;; expect: (i32:1)

(module
    (func (export "ge") (result i32)
        f64.const -42.0
        f64.const -42.0
        f64.ge
    )
)
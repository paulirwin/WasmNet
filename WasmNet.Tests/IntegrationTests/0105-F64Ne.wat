;; invoke: ne
;; expect: (i32:1)

(module
    (func (export "ne") (result i32)
        f64.const -42.0
        f64.const 2.1
        f64.ne
    )
)
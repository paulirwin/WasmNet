;; invoke: ne
;; expect: (i32:1)

(module
    (func (export "ne") (result i32)
        i64.const -4200000000000000000
        i64.const 2
        i64.ne
    )
)
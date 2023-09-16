;; invoke: shru
;; expect: (i64:4611686018427387893)

(module
    (func (export "shru") (result i64)
        i64.const -42
        i64.const 2
        i64.shr_u
    )
)